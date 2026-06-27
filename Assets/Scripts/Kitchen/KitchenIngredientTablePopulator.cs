using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
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
        [SerializeField] private Vector3 sourceLocalStart = new Vector3(-2.8f, 2.2f, -0.2f);
        [SerializeField] private Vector2 sourceSpacing = new Vector2(1.05f, -0.85f);
        [SerializeField] private int itemsPerRow = 6;
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

            int seaweedIndex = 0;
            int riceIndex = 0;
            int fillingIndex = 0;
            foreach (KitchenIngredientCatalogItem item in catalog.SourceItems)
            {
                switch (item.Category)
                {
                    case KitchenIngredientCategory.Seaweed:
                        SpawnSource(item, seaweedTableRoot, seaweedIndex);
                        seaweedIndex++;
                        break;
                    case KitchenIngredientCategory.Rice:
                        SpawnSource(item, riceTableRoot, riceIndex);
                        riceIndex++;
                        break;
                    case KitchenIngredientCategory.Filling:
                        SpawnSource(item, fillingTableRoot, fillingIndex);
                        fillingIndex++;
                        break;
                }
            }
        }

        private void SpawnSource(KitchenIngredientCatalogItem item, Transform parent, int index)
        {
            KitchenIngredientSource source = Instantiate(sourcePrefab, parent);
            source.name = $"{item.DisplayName} Source";
            source.transform.localPosition = GetSourceLocalPosition(index);
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

        private Vector3 GetSourceLocalPosition(int index)
        {
            int columns = Mathf.Max(1, itemsPerRow);
            int row = index / columns;
            int column = index % columns;
            return sourceLocalStart + new Vector3(sourceSpacing.x * column, sourceSpacing.y * row, 0f);
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
            var asdf = new List<Vector3>();
            var qwer=new List<Color>();
            asdf.Add(seaweedTableRoot.position);
            asdf.Add(riceTableRoot.position);
            asdf.Add(fillingTableRoot.position);
            qwer.Add(Color.green);
            qwer.Add(Color.red);
            qwer.Add(Color.yellow);
            for (int i = 0; i < asdf.Count; i++)
            {
                Gizmos.color = qwer[i];
                var a = sourceLocalStart+asdf[i];
                for (int j = 0; j < itemsPerRow; j++)
                {
                    var b=new Vector3(sourceSpacing.x*(j), 0f, 0f);
                    Gizmos.DrawWireCube(a +b, sourceLocalScale);
                }
                //camera
                Gizmos.color = Color.cyan;
                float height = targetCamera.orthographicSize * 2f;
                float width = height * targetCamera.aspect;
                Gizmos.DrawWireCube(asdf[i], new Vector3(width, height, 0f));
            }
            {
                float height = targetCamera.orthographicSize * 2f;
                float width = height * targetCamera.aspect;
                Gizmos.DrawWireCube(CompleteTableRoot.position, new Vector3(width, height, 0f));
                
            }
            
            
        }
    }
}
