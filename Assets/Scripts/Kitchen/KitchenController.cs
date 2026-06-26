using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KimbapGame.Data;
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

        private readonly PreparedKimbapData preparedKimbap = new PreparedKimbapData();
        private readonly PlayerKimbap playerKimbap = new PlayerKimbap();
        private CurrentOrder currentOrder;
        private string sessionId;
        private readonly List<string> tableSequence = new List<string>();

        public PreparedKimbapData PreparedKimbap => preparedKimbap;

        public PlayerKimbap PlayerKimbap => playerKimbap;

        public int MaxSeaweedLayers => maxSeaweedLayers;

        public int MaxRiceLayers => maxRiceLayers;

        public int MaxFillingItems => maxFillingItems;

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
            string path = Path.Combine(Application.persistentDataPath, resultFileName);
            SavePreparedKimbap(path);
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

        public string GetDebugSummary()
        {
            return string.Join(", ", preparedKimbap.AllItems.Select(item => item.ToExportString()).ToArray());
        }
    }
}
