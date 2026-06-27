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
        [SerializeField] private GameObject riceSurfacePrefab;
        [SerializeField] private string resultFileName = "KimbapResults.xlsx";
        [SerializeField] private int droppedSortingBase = 30;
        [SerializeField] private float riceSurfaceLocalZ = -0.05f;
        [SerializeField] private bool logIngredientRegistrationDebug;
        [SerializeField] private int debugDroppedFillingCount;
        [SerializeField] private string debugTopSeaweedName = string.Empty;
        [SerializeField] private string debugLastRegisteredObjectName = string.Empty;

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

        public bool LogIngredientRegistrationDebug => logIngredientRegistrationDebug;

        public int DebugDroppedFillingCount => debugDroppedFillingCount;

        public string DebugTopSeaweedName => debugTopSeaweedName;

        public string DebugLastRegisteredObjectName => debugLastRegisteredObjectName;

        public void ConfigureSceneReferences(KitchenDropZone dropZone, KitchenRicePaintBridge ricePaintBridge)
        {
            this.dropZone = dropZone;
            this.ricePaintBridge = ricePaintBridge;
        }

        public void ConfigureRiceSurfacePrefabForTests(GameObject prefab)
        {
            riceSurfacePrefab = prefab;
        }

        private void Awake()
        {
            sessionId = Guid.NewGuid().ToString("N");
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
            RefreshRegistrationDebugFields(null);
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
                LogRegistrationDebug(
                    $"RegisterDroppedObject Seaweed top='{topSeaweedObject.name}' sorting={sortingOrder}",
                    droppedObject);
            }
            else if (definition != null && definition.Category == KitchenIngredientCategory.Filling && droppedObject != null)
            {
                droppedFillingObjects.Add(droppedObject);
                LogRegistrationDebug(
                    $"RegisterDroppedObject Filling object='{droppedObject.name}' DroppedFillingObjects count={droppedFillingObjects.Count} sorting={sortingOrder}",
                    droppedObject);
            }
            else
            {
                LogRegistrationDebug(
                    $"RegisterDroppedObject category='{GetCategoryName(definition)}' is not tracked by roll fillings. object='{GetObjectName(droppedObject)}' sorting={sortingOrder}",
                    droppedObject);
            }

            RefreshRegistrationDebugFields(droppedObject);
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

            if (riceSurfacePrefab == null)
            {
                Debug.LogWarning("KitchenController requires a rice surface prefab before rice can be painted.");
                return;
            }

            GameObject surfaceObject = Instantiate(riceSurfacePrefab, topSeaweedObject.transform);
            surfaceObject.name = "TopSeaweedRiceSurface";
            surfaceObject.transform.SetParent(topSeaweedObject.transform, false);
            surfaceObject.transform.localPosition = new Vector3(0f, 0f, riceSurfaceLocalZ);
            surfaceObject.transform.localScale = new Vector3(0.92f, 0.78f, 1f);

            SpriteRenderer renderer = surfaceObject.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                if (renderer.sprite == null)
                {
                    renderer.sprite = KitchenPlaceholderFactory.CreateWhiteSprite();
                }

                renderer.sortingOrder = sortingOrder;
            }

            currentRiceSurface = surfaceObject.GetComponent<SpreadableSurface>();
            currentRiceInputController = surfaceObject.GetComponent<SpreadInputController>();
            if (currentRiceSurface == null || currentRiceInputController == null)
            {
                Debug.LogWarning("Rice surface prefab must contain SpreadableSurface and SpreadInputController components.");
                DestroyUnityObject(surfaceObject);
                currentRiceSurface = null;
                currentRiceInputController = null;
                return;
            }

            currentRiceSurface.SetSurfaceColor(surfaceColor);
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

        public void LogRegistrationDebug(string message, UnityEngine.Object context = null)
        {
            if (!logIngredientRegistrationDebug)
            {
                return;
            }

            if (context == null)
            {
                Debug.Log($"[KitchenRegistration] {message}");
            }
            else
            {
                Debug.Log($"[KitchenRegistration] {message}", context);
            }
        }

        public string GetRegistrationDebugSummary(KitchenIngredientDefinition definition)
        {
            if (definition == null)
            {
                return "definition=null";
            }

            switch (definition.Category)
            {
                case KitchenIngredientCategory.Seaweed:
                    return $"seaweed={preparedKimbap.seaweeds.Count}/{maxSeaweedLayers}";
                case KitchenIngredientCategory.Rice:
                    return $"rice={preparedKimbap.riceItems.Count}/{maxRiceLayers}, currentRiceSurface={GetObjectName(currentRiceSurface == null ? null : currentRiceSurface.gameObject)}";
                case KitchenIngredientCategory.Filling:
                    return $"filling={preparedKimbap.fillings.Count}/{maxFillingItems}, DroppedFillingObjects count={droppedFillingObjects.Count}";
                default:
                    return $"category={definition.Category}";
            }
        }

        private void RefreshRegistrationDebugFields(GameObject lastRegisteredObject)
        {
            debugDroppedFillingCount = droppedFillingObjects.Count;
            debugTopSeaweedName = topSeaweedObject == null ? string.Empty : topSeaweedObject.name;
            debugLastRegisteredObjectName = lastRegisteredObject == null ? string.Empty : lastRegisteredObject.name;
        }

        private static string GetCategoryName(KitchenIngredientDefinition definition)
        {
            return definition == null ? "null" : definition.Category.ToString();
        }

        private static string GetObjectName(GameObject target)
        {
            return target == null ? "null" : target.name;
        }
    }
}
