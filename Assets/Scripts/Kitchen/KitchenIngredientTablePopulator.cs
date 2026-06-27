using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace KimbapGame.Kitchen
{
    [DisallowMultipleComponent]
    public sealed class KitchenIngredientTablePopulator : MonoBehaviour
    {
        [SerializeField] private string ingredientsCsvRelativePath = "Kitchen/ingredients.csv";
        [SerializeField] private KitchenIngredientSource sourcePrefab;
        [SerializeField] private GameObject dragPreviewPrefab;
        [SerializeField] private KitchenController controller;
        [SerializeField] private KitchenDropZone dropZone;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform seaweedTableRoot;
        [SerializeField] private Transform riceTableRoot;
        [SerializeField] private Transform fillingTableRoot;
        [SerializeField] private Vector3 sourceRowCenter = new Vector3(0f, 2.2f, -0.2f);
        [SerializeField] private Vector2 sourceSpacing = new Vector2(1.05f, -0.85f);
        [SerializeField, Min(1)] private int seaweedItemsPerRow = 6;
        [SerializeField, Min(1)] private int riceItemsPerRow = 6;
        [SerializeField, Min(1)] private int fillingItemsPerRow = 6;
        [SerializeField] private Vector3 sourceLocalScale = new Vector3(0.78f, 0.48f, 1f);
        [SerializeField] private Vector2 seaweedDragPreviewSize = new Vector2(2.4f, 1.7f);
        [SerializeField] private Vector2 riceDragPreviewSize = new Vector2(2.8f, 0.28f);
        [SerializeField] private Vector2 fillingDragPreviewSize = new Vector2(2.8f, 0.28f);
        [SerializeField] private Color seaweedColor = new Color(0.08f, 0.16f, 0.11f, 1f);
        [SerializeField] private Color riceColor = new Color(1f, 0.97f, 0.86f, 1f);
        [SerializeField] private Color fillingColor = new Color(0.95f, 0.44f, 0.42f, 1f);
        [SerializeField] private Transform CompleteTableRoot;
        private readonly List<KitchenIngredientSource> spawnedSources = new List<KitchenIngredientSource>();

        public IReadOnlyList<KitchenIngredientSource> SpawnedSources => spawnedSources;

        private IEnumerator Start()
        {
            yield return PopulateFromStreamingAssets();
        }

        public IEnumerator PopulateFromStreamingAssets()
        {
            string uri = BuildStreamingAssetsUri();
            UnityWebRequest request = UnityWebRequest.Get(uri);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string error = request.error;
                request.Dispose();
                Debug.LogWarning($"Failed to load kitchen ingredient data from '{uri}': {error}");
                yield break;
            }

            string csv = request.downloadHandler.text;
            request.Dispose();
            PopulateFromCsv(csv);
        }

        public void PopulateFromCsv(string csv)
        {
            Populate(KitchenIngredientCatalog.Parse(csv));
        }

        public void Populate(KitchenIngredientCatalog catalog)
        {
            ClearSpawnedSources();
            if (catalog == null || !HasRequiredReferences())
            {
                return;
            }

            List<KitchenIngredientCatalogItem> seaweedItems = new List<KitchenIngredientCatalogItem>();
            List<KitchenIngredientCatalogItem> riceItems = new List<KitchenIngredientCatalogItem>();
            List<KitchenIngredientCatalogItem> fillingItems = new List<KitchenIngredientCatalogItem>();
            foreach (KitchenIngredientCatalogItem item in catalog.SourceItems)
            {
                switch (item.Category)
                {
                    case KitchenIngredientCategory.Seaweed:
                        seaweedItems.Add(item);
                        break;
                    case KitchenIngredientCategory.Rice:
                        riceItems.Add(item);
                        break;
                    case KitchenIngredientCategory.Filling:
                        fillingItems.Add(item);
                        break;
                }
            }

            SpawnSources(seaweedItems, seaweedTableRoot, KitchenIngredientCategory.Seaweed);
            SpawnSources(riceItems, riceTableRoot, KitchenIngredientCategory.Rice);
            SpawnSources(fillingItems, fillingTableRoot, KitchenIngredientCategory.Filling);
        }

        private void SpawnSources(
            IReadOnlyList<KitchenIngredientCatalogItem> items,
            Transform parent,
            KitchenIngredientCategory category)
        {
            int columns = GetItemsPerRow(category);
            for (int i = 0; i < items.Count; i++)
            {
                SpawnSource(items[i], parent, i, items.Count, columns);
            }
        }

        private void SpawnSource(KitchenIngredientCatalogItem item, Transform parent, int index, int totalCount, int columns)
        {
            KitchenIngredientSource source = Instantiate(sourcePrefab, parent);
            source.name = $"{item.DisplayName} Source";
            source.transform.localPosition = GetSourceLocalPosition(index, totalCount, columns);
            source.transform.localRotation = Quaternion.identity;
            source.transform.localScale = sourceLocalScale;
            source.Configure(
                item.ToDefinition(dragPreviewPrefab, GetPlaceholderColor(item.Category)),
                controller,
                dropZone,
                targetCamera,
                GetDragPreviewSize(item.Category));
            spawnedSources.Add(source);
        }

        private Vector3 GetSourceLocalPosition(int index, int totalCount, int columns)
        {
            columns = Mathf.Max(1, columns);
            int row = index / columns;
            int column = index % columns;
            int rowItemCount = Mathf.Min(columns, Mathf.Max(1, totalCount - (row * columns)));
            float rowWidth = (rowItemCount - 1) * sourceSpacing.x;
            float x = sourceRowCenter.x - (rowWidth * 0.5f) + (sourceSpacing.x * column);
            int rowCount = Mathf.CeilToInt(totalCount / (float)columns);
            float rowOffset = row - ((rowCount - 1) * 0.5f);
            float y = sourceRowCenter.y + (sourceSpacing.y * rowOffset);
            return new Vector3(x, y, sourceRowCenter.z);
        }

        private int GetItemsPerRow(KitchenIngredientCategory category)
        {
            switch (category)
            {
                case KitchenIngredientCategory.Seaweed:
                    return Mathf.Max(1, seaweedItemsPerRow);
                case KitchenIngredientCategory.Rice:
                    return Mathf.Max(1, riceItemsPerRow);
                case KitchenIngredientCategory.Filling:
                    return Mathf.Max(1, fillingItemsPerRow);
                default:
                    return 1;
            }
        }

        private Vector2 GetDragPreviewSize(KitchenIngredientCategory category)
        {
            switch (category)
            {
                case KitchenIngredientCategory.Seaweed:
                    return seaweedDragPreviewSize;
                case KitchenIngredientCategory.Rice:
                    return riceDragPreviewSize;
                default:
                    return fillingDragPreviewSize;
            }
        }

        private Color GetPlaceholderColor(KitchenIngredientCategory category)
        {
            switch (category)
            {
                case KitchenIngredientCategory.Seaweed:
                    return seaweedColor;
                case KitchenIngredientCategory.Rice:
                    return riceColor;
                default:
                    return fillingColor;
            }
        }

        private bool HasRequiredReferences()
        {
            bool hasReferences = sourcePrefab != null
                && dragPreviewPrefab != null
                && controller != null
                && dropZone != null
                && targetCamera != null
                && seaweedTableRoot != null
                && riceTableRoot != null
                && fillingTableRoot != null;

            if (!hasReferences)
            {
                Debug.LogWarning("KitchenIngredientTablePopulator is missing one or more serialized references.");
            }

            return hasReferences;
        }

        private string BuildStreamingAssetsUri()
        {
            string path = Path.Combine(Application.streamingAssetsPath, ingredientsCsvRelativePath).Replace("\\", "/");
            return path.Contains("://") ? path : new Uri(path).AbsoluteUri;
        }

        private void ClearSpawnedSources()
        {
            for (int i = 0; i < spawnedSources.Count; i++)
            {
                KitchenIngredientSource source = spawnedSources[i];
                if (source != null)
                {
                    DestroyUnityObject(source.gameObject);
                }
            }

            spawnedSources.Clear();
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

        private void OnDrawGizmos()
        {
            KitchenIngredientCatalog catalog = TryLoadLocalCatalogForGizmos();
            DrawTableGizmos(seaweedTableRoot, KitchenIngredientCategory.Seaweed, catalog, Color.green);
            DrawTableGizmos(riceTableRoot, KitchenIngredientCategory.Rice, catalog, Color.red);
            DrawTableGizmos(fillingTableRoot, KitchenIngredientCategory.Filling, catalog, Color.yellow);
            DrawCameraFrameGizmo(CompleteTableRoot);
        }

        private void DrawTableGizmos(
            Transform tableRoot,
            KitchenIngredientCategory category,
            KitchenIngredientCatalog catalog,
            Color sourceColor)
        {
            if (tableRoot == null)
            {
                return;
            }

            int sourceCount = GetGizmoSourceCount(tableRoot, category, catalog);
            int columns = GetItemsPerRow(category);
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.matrix = tableRoot.localToWorldMatrix;
            Gizmos.color = sourceColor;
            for (int i = 0; i < sourceCount; i++)
            {
                Gizmos.DrawWireCube(GetSourceLocalPosition(i, sourceCount, columns), sourceLocalScale);
            }

            Gizmos.matrix = previousMatrix;
            DrawCameraFrameGizmo(tableRoot);
        }

        private int GetGizmoSourceCount(
            Transform tableRoot,
            KitchenIngredientCategory category,
            KitchenIngredientCatalog catalog)
        {
            if (tableRoot.childCount > 0)
            {
                return tableRoot.childCount;
            }

            if (catalog == null)
            {
                return GetItemsPerRow(category);
            }

            int count = 0;
            foreach (KitchenIngredientCatalogItem item in catalog.SourceItems)
            {
                if (item.Category == category)
                {
                    count++;
                }
            }

            return Mathf.Max(1, count);
        }

        private KitchenIngredientCatalog TryLoadLocalCatalogForGizmos()
        {
            string path = Path.Combine(Application.streamingAssetsPath, ingredientsCsvRelativePath);
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                return KitchenIngredientCatalog.Parse(File.ReadAllText(path));
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void DrawCameraFrameGizmo(Transform center)
        {
            if (center == null || targetCamera == null)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            float height = targetCamera.orthographicSize * 2f;
            float width = height * targetCamera.aspect;
            Gizmos.DrawWireCube(center.position, new Vector3(width, height, 0f));
        }
    }
}
