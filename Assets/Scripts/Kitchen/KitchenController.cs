using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameJam.Gameplay.Spreading;
using KimbapGame.Data;
using KimbapGame.Evaluation;
using UnityEngine;

namespace KimbapGame.Kitchen
{
    public sealed class KitchenController : MonoBehaviour
    {
        [SerializeField] private int maxSeaweedLayers = 5;
        [SerializeField] private int maxRiceLayers = 5;
        [SerializeField] private int maxFillingItems = 10;
        [SerializeField] private KitchenDropZone dropZone;
        [SerializeField] private KitchenRicePaintBridge ricePaintBridge;
        [SerializeField] private string resultFileName = "KimbapResults.xlsx";
        [SerializeField] private int droppedSortingBase = 30;
        [SerializeField] private float riceSurfaceLocalZ = -0.05f;

        private readonly PreparedKimbapData preparedKimbap = new PreparedKimbapData();
        private readonly PlayerKimbap playerKimbap = new PlayerKimbap();
        private CurrentOrder currentOrder;
        private string sessionId;
        private readonly List<string> tableSequence = new List<string>();
        private readonly List<GameObject> droppedFillingObjects = new List<GameObject>();
        private GameObject topSeaweedObject;
        private SpreadableSurface currentRiceSurface;
        private SpreadInputController currentRiceInputController;
        private int droppedLayerIndex;
        private bool hasSavedCurrentKimbap;
        private string lastSavedPath;

        public PreparedKimbapData PreparedKimbap => preparedKimbap;

        public PlayerKimbap PlayerKimbap => playerKimbap;

        public int MaxSeaweedLayers => maxSeaweedLayers;

        public int MaxRiceLayers => maxRiceLayers;

        public int MaxFillingItems => maxFillingItems;

        public GameObject TopSeaweedObject => topSeaweedObject;

        public SpreadableSurface CurrentRiceSurface => currentRiceSurface;

        public SpreadInputController CurrentRiceInputController => currentRiceInputController;

        public IReadOnlyList<GameObject> DroppedFillingObjects => droppedFillingObjects;

        public void ConfigureSceneReferences(KitchenDropZone dropZone, KitchenRicePaintBridge ricePaintBridge)
        {
            this.dropZone = dropZone;
            this.ricePaintBridge = ricePaintBridge;
        }

        private void Awake()
        {
            sessionId = Guid.NewGuid().ToString("N");

            if (dropZone == null)
            {
                dropZone = FindObjectOfType<KitchenDropZone>();
            }

            if (ricePaintBridge == null)
            {
                ricePaintBridge = FindObjectOfType<KitchenRicePaintBridge>();
            }
        }

        public void SetCurrentOrder(CurrentOrder order)
        {
            currentOrder = order;
        }

        public void ConfigureLimitsForTests(int seaweedLimit, int riceLimit, int fillingLimit)
        {
            maxSeaweedLayers = Mathf.Max(0, seaweedLimit);
            maxRiceLayers = Mathf.Max(0, riceLimit);
            maxFillingItems = Mathf.Max(0, fillingLimit);
        }

        public void ResetPreparation()
        {
            preparedKimbap.Clear();
            playerKimbap.Clear();
            tableSequence.Clear();
            droppedFillingObjects.Clear();
            DestroyCurrentRiceSurface();
            topSeaweedObject = null;
            droppedLayerIndex = 0;
            hasSavedCurrentKimbap = false;
            lastSavedPath = string.Empty;
        }

        public bool TryAddIngredient(KitchenIngredientDefinition definition)
        {
            if (definition == null)
            {
                return false;
            }

            switch (definition.Category)
            {
                case KitchenIngredientCategory.Seaweed:
                    return TryAddSeaweed(definition);
                case KitchenIngredientCategory.Rice:
                    return TryAddRice(definition);
                case KitchenIngredientCategory.Filling:
                    return TryAddFilling(definition);
                default:
                    return false;
            }
        }

        public bool TryAddSeaweed(KitchenIngredientDefinition definition)
        {
            if (!CanAdd(preparedKimbap.seaweeds.Count, maxSeaweedLayers, definition))
            {
                return false;
            }

            AddPreparedItem(preparedKimbap.seaweeds, definition);
            tableSequence.Add("Seaweed");
            return true;
        }

        public bool TryAddRice(KitchenIngredientDefinition definition)
        {
            if (currentRiceSurface == null || currentRiceInputController == null)
            {
                return false;
            }

            if (!CanAdd(preparedKimbap.riceItems.Count, maxRiceLayers, definition))
            {
                return false;
            }

            AddPreparedItem(preparedKimbap.riceItems, definition);
            tableSequence.Add("Rice");
            return true;
        }

        public bool TryAddFilling(KitchenIngredientDefinition definition)
        {
            if (!CanAdd(preparedKimbap.fillings.Count, maxFillingItems, definition))
            {
                return false;
            }

            AddPreparedItem(preparedKimbap.fillings, definition);
            tableSequence.Add("Filling");
            return true;
        }

        public int RegisterDroppedObject(KitchenIngredientDefinition definition, GameObject droppedObject)
        {
            int sortingOrder = droppedSortingBase + (droppedLayerIndex * 3);
            droppedLayerIndex++;

            ApplySortingOrder(droppedObject, sortingOrder);

            if (definition != null && definition.Category == KitchenIngredientCategory.Seaweed && droppedObject != null)
            {
                DestroyCurrentRiceSurface();
                topSeaweedObject = droppedObject;
                BuildRiceSurfaceOnTopSeaweed(definition.PlaceholderColor, sortingOrder + 1);
            }
            else if (definition != null && definition.Category == KitchenIngredientCategory.Filling && droppedObject != null)
            {
                droppedFillingObjects.Add(droppedObject);
            }

            return sortingOrder;
        }

        public void SelectRice(KitchenIngredientDefinition definition)
        {
            if (definition == null || ricePaintBridge == null)
            {
                return;
            }

            ricePaintBridge.SelectRice(definition, this);
        }

        public string CompleteAndSave()
        {
            if (hasSavedCurrentKimbap && !string.IsNullOrEmpty(lastSavedPath))
            {
                return lastSavedPath;
            }

            string path = Path.Combine(Application.persistentDataPath, resultFileName);
            SavePreparedKimbap(path);
            SharedOrderContext.SetEvaluationResult(KimbapEvaluator.Evaluate(SharedOrderContext.CurrentSheetOrder, preparedKimbap));
            SharedOrderContext.CompleteCurrentOrder();
            hasSavedCurrentKimbap = true;
            lastSavedPath = path;
            Debug.Log($"Saved prepared kimbap data to {path}");
            return path;
        }

        public void SavePreparedKimbap(string path)
        {
            var row = new KimbapResultRow(
                sessionId,
                DateTime.UtcNow,
                currentOrder == null ? null : currentOrder.order,
                string.Join(">", tableSequence),
                preparedKimbap);
            KimbapXlsxWriter.Write(path, row);
        }

        private void AddPreparedItem(List<PreparedKimbapItem> target, KitchenIngredientDefinition definition)
        {
            target.Add(new PreparedKimbapItem(
                definition.IngredientType,
                definition.Category,
                definition.VariantId,
                definition.DisplayName));
            playerKimbap.AddIngredient(definition.IngredientType);
        }

        private static bool CanAdd(int currentCount, int maxCount, KitchenIngredientDefinition definition)
        {
            return definition != null && maxCount > 0 && currentCount < maxCount;
        }

        private void BuildRiceSurfaceOnTopSeaweed(Color surfaceColor, int sortingOrder)
        {
            if (topSeaweedObject == null)
            {
                return;
            }

            GameObject surfaceObject = new GameObject("TopSeaweedRiceSurface");
            surfaceObject.transform.SetParent(topSeaweedObject.transform, false);
            surfaceObject.transform.localPosition = new Vector3(0f, 0f, riceSurfaceLocalZ);
            surfaceObject.transform.localScale = new Vector3(0.92f, 0.78f, 1f);

            SpriteRenderer renderer = surfaceObject.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;

            currentRiceSurface = surfaceObject.AddComponent<SpreadableSurface>();
            currentRiceSurface.SetSurfaceColor(surfaceColor);
            currentRiceInputController = surfaceObject.AddComponent<SpreadInputController>();
            currentRiceInputController.enabled = false;
        }

        private void DestroyCurrentRiceSurface()
        {
            GameObject surfaceObject = currentRiceSurface == null ? null : currentRiceSurface.gameObject;
            currentRiceSurface = null;
            currentRiceInputController = null;

            if (surfaceObject != null)
            {
                DestroyUnityObject(surfaceObject);
            }
        }

        private static void ApplySortingOrder(GameObject target, int sortingOrder)
        {
            if (target == null)
            {
                return;
            }

            SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sortingOrder = sortingOrder + i;
            }
        }

        private static void DestroyUnityObject(UnityEngine.Object target)
        {
            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        public string GetDebugSummary()
        {
            return string.Join(", ", preparedKimbap.AllItems.Select(item => item.ToExportString()).ToArray());
        }
    }
}
