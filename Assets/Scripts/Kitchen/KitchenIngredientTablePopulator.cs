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
        [Serializable]
        public sealed class TableSourceSettings
        {
            [SerializeField] private Transform tableRoot;
            [SerializeField] private KitchenIngredientSource sourcePrefab;
            [SerializeField] private Vector3 rowCenter = new Vector3(0f, 2.2f, -0.2f);
            [SerializeField] private Vector2 spacing = new Vector2(1.05f, -0.85f);
            [SerializeField, Min(1)] private int itemsPerRow = 6;
            [SerializeField] private Vector3 localScale = new Vector3(0.78f, 0.48f, 1f);
            [SerializeField] private Vector2 dragPreviewSize = new Vector2(2.2f, 0.45f);
            [SerializeField] private Color placeholderColor = Color.white;
            [SerializeField] private int sourceSortingOrderBase = 4;
            [SerializeField, Min(1)] private int sourceSortingOrderStep = 10;

            public Transform TableRoot => tableRoot;
            public KitchenIngredientSource SourcePrefab => sourcePrefab;
            public Vector3 LocalScale => localScale;
            public Vector2 DragPreviewSize => dragPreviewSize;
            public Color PlaceholderColor => placeholderColor;
            public int SourceSortingOrderBase => sourceSortingOrderBase;
            public int SourceSortingOrderStep => Mathf.Max(1, sourceSortingOrderStep);
            public int ItemsPerRow => Mathf.Max(1, itemsPerRow);
            public bool HasRequiredReferences => tableRoot != null && sourcePrefab != null;

            public static TableSourceSettings Create(Vector2 dragPreviewSize, Color placeholderColor)
            {
                return new TableSourceSettings
                {
                    dragPreviewSize = dragPreviewSize,
                    placeholderColor = placeholderColor
                };
            }

            public void ApplyLegacyIfEmpty(
                Transform legacyTableRoot,
                KitchenIngredientSource legacySourcePrefab,
                Vector3 legacyRowCenter,
                Vector2 legacySpacing,
                int legacyItemsPerRow,
                Vector3 legacyLocalScale,
                Vector2 legacyDragPreviewSize,
                Color legacyPlaceholderColor)
            {
                if (tableRoot != null || sourcePrefab != null)
                {
                    return;
                }

                tableRoot = legacyTableRoot;
                sourcePrefab = legacySourcePrefab;
                rowCenter = legacyRowCenter;
                spacing = legacySpacing;
                itemsPerRow = Mathf.Max(1, legacyItemsPerRow);
                localScale = legacyLocalScale;
                dragPreviewSize = legacyDragPreviewSize;
                placeholderColor = legacyPlaceholderColor;
            }

            public Vector3 GetSourceLocalPosition(int index, int totalCount)
            {
                int columns = ItemsPerRow;
                int row = index / columns;
                int column = index % columns;
                int rowItemCount = Mathf.Min(columns, Mathf.Max(1, totalCount - (row * columns)));
                float rowWidth = (rowItemCount - 1) * spacing.x;
                float x = rowCenter.x - (rowWidth * 0.5f) + (spacing.x * column);
                int rowCount = Mathf.CeilToInt(totalCount / (float)columns);
                float rowOffset = row - ((rowCount - 1) * 0.5f);
                float y = rowCenter.y + (spacing.y * rowOffset);
                return new Vector3(x, y, rowCenter.z);
            }

            public int GetSourceSortingOrder(int index)
            {
                return sourceSortingOrderBase + (Mathf.Max(0, index) * SourceSortingOrderStep);
            }
        }

        [SerializeField] private string ingredientsCsvRelativePath = "Kitchen/ingredients.csv";
        [SerializeField] private TableSourceSettings seaweedSettings = TableSourceSettings.Create(
            new Vector2(2.4f, 1.7f),
            new Color(0.08f, 0.16f, 0.11f, 1f));
        [SerializeField] private TableSourceSettings riceSettings = TableSourceSettings.Create(
            new Vector2(2.8f, 0.28f),
            new Color(1f, 0.97f, 0.86f, 1f));
        [SerializeField] private TableSourceSettings fillingSettings = TableSourceSettings.Create(
            new Vector2(2.8f, 0.28f),
            new Color(0.95f, 0.44f, 0.42f, 1f));
        [SerializeField] private GameObject dragPreviewPrefab;
        [SerializeField] private KitchenController controller;
        [SerializeField] private KitchenDropZone dropZone;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform CompleteTableRoot;
        [SerializeField, HideInInspector] private KitchenIngredientSource sourcePrefab;
        [SerializeField, HideInInspector] private Transform seaweedTableRoot;
        [SerializeField, HideInInspector] private Transform riceTableRoot;
        [SerializeField, HideInInspector] private Transform fillingTableRoot;
        [SerializeField, HideInInspector] private Vector3 sourceRowCenter = new Vector3(0f, 2.2f, -0.2f);
        [SerializeField, HideInInspector] private Vector2 sourceSpacing = new Vector2(1.05f, -0.85f);
        [SerializeField, HideInInspector, Min(1)] private int seaweedItemsPerRow = 6;
        [SerializeField, HideInInspector, Min(1)] private int riceItemsPerRow = 6;
        [SerializeField, HideInInspector, Min(1)] private int fillingItemsPerRow = 6;
        [SerializeField, HideInInspector] private Vector3 sourceLocalScale = new Vector3(0.78f, 0.48f, 1f);
        [SerializeField, HideInInspector] private Vector2 seaweedDragPreviewSize = new Vector2(2.4f, 1.7f);
        [SerializeField, HideInInspector] private Vector2 riceDragPreviewSize = new Vector2(2.8f, 0.28f);
        [SerializeField, HideInInspector] private Vector2 fillingDragPreviewSize = new Vector2(2.8f, 0.28f);
        [SerializeField, HideInInspector] private Color seaweedColor = new Color(0.08f, 0.16f, 0.11f, 1f);
        [SerializeField, HideInInspector] private Color riceColor = new Color(1f, 0.97f, 0.86f, 1f);
        [SerializeField, HideInInspector] private Color fillingColor = new Color(0.95f, 0.44f, 0.42f, 1f);

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
            EnsureTableSettings();
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

            SpawnSources(seaweedItems, seaweedSettings);
            SpawnSources(riceItems, riceSettings);
            SpawnSources(fillingItems, fillingSettings);
        }

        private void SpawnSources(IReadOnlyList<KitchenIngredientCatalogItem> items, TableSourceSettings settings)
        {
            if (settings == null || !settings.HasRequiredReferences)
            {
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                SpawnSource(items[i], settings, i, items.Count);
            }
        }

        private void SpawnSource(KitchenIngredientCatalogItem item, TableSourceSettings settings, int index, int totalCount)
        {
            KitchenIngredientSource source = Instantiate(settings.SourcePrefab, settings.TableRoot);
            source.name = $"{item.DisplayName} Source";
            source.transform.localPosition = settings.GetSourceLocalPosition(index, totalCount);
            source.transform.localRotation = Quaternion.identity;
            source.transform.localScale = settings.LocalScale;
            source.Configure(
                item.ToDefinition(dragPreviewPrefab, settings.PlaceholderColor),
                controller,
                dropZone,
                targetCamera,
                settings.DragPreviewSize);
            ApplySourceSortingOrder(source.gameObject, settings.GetSourceSortingOrder(index));
            spawnedSources.Add(source);
        }

        private static void ApplySourceSortingOrder(GameObject sourceObject, int baseSortingOrder)
        {
            if (sourceObject == null)
            {
                return;
            }

            SpriteRenderer[] renderers = sourceObject.GetComponentsInChildren<SpriteRenderer>();
            if (renderers.Length == 0)
            {
                return;
            }

            int minimumSortingOrder = renderers[0].sortingOrder;
            for (int i = 1; i < renderers.Length; i++)
            {
                minimumSortingOrder = Mathf.Min(minimumSortingOrder, renderers[i].sortingOrder);
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sortingOrder = baseSortingOrder + (renderers[i].sortingOrder - minimumSortingOrder);
            }
        }

        private bool HasRequiredReferences()
        {
            EnsureTableSettings();
            bool hasReferences = dragPreviewPrefab != null
                && controller != null
                && dropZone != null
                && targetCamera != null
                && seaweedSettings != null
                && riceSettings != null
                && fillingSettings != null
                && seaweedSettings.HasRequiredReferences
                && riceSettings.HasRequiredReferences
                && fillingSettings.HasRequiredReferences;

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

        private void EnsureTableSettings()
        {
            if (seaweedSettings == null)
            {
                seaweedSettings = TableSourceSettings.Create(seaweedDragPreviewSize, seaweedColor);
            }

            if (riceSettings == null)
            {
                riceSettings = TableSourceSettings.Create(riceDragPreviewSize, riceColor);
            }

            if (fillingSettings == null)
            {
                fillingSettings = TableSourceSettings.Create(fillingDragPreviewSize, fillingColor);
            }

            seaweedSettings.ApplyLegacyIfEmpty(
                seaweedTableRoot,
                sourcePrefab,
                sourceRowCenter,
                sourceSpacing,
                seaweedItemsPerRow,
                sourceLocalScale,
                seaweedDragPreviewSize,
                seaweedColor);
            riceSettings.ApplyLegacyIfEmpty(
                riceTableRoot,
                sourcePrefab,
                sourceRowCenter,
                sourceSpacing,
                riceItemsPerRow,
                sourceLocalScale,
                riceDragPreviewSize,
                riceColor);
            fillingSettings.ApplyLegacyIfEmpty(
                fillingTableRoot,
                sourcePrefab,
                sourceRowCenter,
                sourceSpacing,
                fillingItemsPerRow,
                sourceLocalScale,
                fillingDragPreviewSize,
                fillingColor);
        }

        private void OnDrawGizmos()
        {
            EnsureTableSettings();
            KitchenIngredientCatalog catalog = TryLoadLocalCatalogForGizmos();
            DrawTableGizmos(seaweedSettings, KitchenIngredientCategory.Seaweed, catalog, Color.green);
            DrawTableGizmos(riceSettings, KitchenIngredientCategory.Rice, catalog, Color.red);
            DrawTableGizmos(fillingSettings, KitchenIngredientCategory.Filling, catalog, Color.yellow);
            DrawCameraFrameGizmo(CompleteTableRoot);
        }

        private void DrawTableGizmos(
            TableSourceSettings settings,
            KitchenIngredientCategory category,
            KitchenIngredientCatalog catalog,
            Color sourceColor)
        {
            if (settings == null || settings.TableRoot == null)
            {
                return;
            }

            int sourceCount = GetGizmoSourceCount(settings, category, catalog);
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.matrix = settings.TableRoot.localToWorldMatrix;
            Gizmos.color = sourceColor;
            for (int i = 0; i < sourceCount; i++)
            {
                Gizmos.DrawWireCube(settings.GetSourceLocalPosition(i, sourceCount), settings.LocalScale);
            }

            Gizmos.matrix = previousMatrix;
            DrawCameraFrameGizmo(settings.TableRoot);
        }

        private int GetGizmoSourceCount(
            TableSourceSettings settings,
            KitchenIngredientCategory category,
            KitchenIngredientCatalog catalog)
        {
            if (settings.TableRoot.childCount > 0)
            {
                return settings.TableRoot.childCount;
            }

            if (catalog == null)
            {
                return settings.ItemsPerRow;
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
