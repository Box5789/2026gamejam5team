using System.IO;
using KimbapGame.Kitchen;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KimbapGame.Tests.Kitchen
{
    public sealed class KitchenSceneWiringTests
    {
        private const string KitchenScenePath = "Assets/Scenes/kitchen.unity";
        private const string SourcePrefabPath = "Assets/Prefabs/Kitchen/KitchenIngredientSource.prefab";
        private const string DragPreviewPrefabPath = "Assets/Prefabs/Kitchen/KitchenDragPreview.prefab";
        private const string HandCursorPrefabPath = "Assets/Prefabs/Kitchen/KitchenHandCursor.prefab";
        private const string RemovedBootstrapGuid = "a6cdb1e799334b2c9ee5cd48966b7eca";

        [Test]
        public void KitchenScene_UsesSerializedSceneObjectsInsteadOfBootstrap()
        {
            string sceneText = File.ReadAllText(KitchenScenePath);
            Assert.IsFalse(sceneText.Contains("Kitchen" + "SceneBootstrap"));
            Assert.IsFalse(sceneText.Contains(RemovedBootstrapGuid));

            EditorSceneManager.OpenScene(KitchenScenePath, OpenSceneMode.Single);

            KitchenController controller = Object.FindFirstObjectByType<KitchenController>();
            KitchenDropZone dropZone = Object.FindFirstObjectByType<KitchenDropZone>();
            KitchenRicePaintBridge ricePaintBridge = Object.FindFirstObjectByType<KitchenRicePaintBridge>();
            KitchenTableNavigator navigator = Object.FindFirstObjectByType<KitchenTableNavigator>();
            KitchenRollAnimator rollAnimator = Object.FindFirstObjectByType<KitchenRollAnimator>();
            KitchenIngredientTablePopulator populator = Object.FindFirstObjectByType<KitchenIngredientTablePopulator>();
            KitchenHandCursor handCursor = Object.FindFirstObjectByType<KitchenHandCursor>();
            KitchenIngredientSource[] sources = Object.FindObjectsByType<KitchenIngredientSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            Assert.IsNotNull(controller);
            Assert.IsNotNull(dropZone);
            Assert.IsNotNull(ricePaintBridge);
            Assert.IsNotNull(navigator);
            Assert.IsNotNull(rollAnimator);
            Assert.IsNotNull(populator);
            Assert.IsNotNull(handCursor);
            Assert.AreEqual(0, sources.Length, "Ingredient source scene instances should be spawned from local CSV at runtime.");

            AssertObjectReference(controller, "dropZone");
            AssertObjectReference(controller, "ricePaintBridge");
            AssertObjectReference(controller, "riceSurfacePrefab");
            AssertObjectReference(navigator, "movingTablesRoot");
            AssertObjectReference(navigator, "nextButton");
            AssertObjectReference(navigator, "ricePaintBridge");
            AssertObjectReference(navigator, "ricePaintingTableRoot");
            AssertObjectReference(rollAnimator, "controller");
            AssertObjectReference(rollAnimator, "targetCamera");
            AssertObjectReference(rollAnimator, "completeButton");
            AssertObjectReference(rollAnimator, "submitButton");
            AssertObjectReference(populator, "sourcePrefab");
            AssertObjectReference(populator, "dragPreviewPrefab");
            AssertObjectReference(populator, "controller");
            AssertObjectReference(populator, "dropZone");
            AssertObjectReference(populator, "targetCamera");
            AssertObjectReference(populator, "seaweedTableRoot");
            AssertObjectReference(populator, "riceTableRoot");
            AssertObjectReference(populator, "fillingTableRoot");
            AssertPositiveIntValue(populator, "seaweedItemsPerRow");
            AssertPositiveIntValue(populator, "riceItemsPerRow");
            AssertPositiveIntValue(populator, "fillingItemsPerRow");
            AssertStringValue(populator, "ingredientsCsvRelativePath", "Kitchen/ingredients.csv");
            AssertPrefabReferencePath(populator, "sourcePrefab", SourcePrefabPath);
            AssertObjectReference(handCursor, "targetCamera");
            AssertObjectReference(handCursor, "armPivot");
            AssertObjectReference(handCursor, "handPivot");
            AssertObjectReference(handCursor, "armRoot");
            AssertObjectReference(handCursor, "handVisualRoot");
            AssertObjectReference(handCursor, "armVisualRoot");
            AssertObjectReference(handCursor, "handRenderer");
            AssertPrefabInstanceRootPath(handCursor.gameObject, HandCursorPrefabPath);
            AssertNavigatorTableRoots(navigator);
            AssertDragPreviewPrefabHasRollSeaweedCover();
        }

        private static void AssertObjectReference(Object target, string propertyPath)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyPath);
            Assert.IsNotNull(property, $"{target.name}.{propertyPath} is missing.");
            Assert.IsNotNull(property.objectReferenceValue, $"{target.name}.{propertyPath} is not wired.");
        }

        private static void AssertStringValue(Object target, string propertyPath, string expected)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyPath);
            Assert.IsNotNull(property, $"{target.name}.{propertyPath} is missing.");
            Assert.AreEqual(expected, property.stringValue);
        }

        private static void AssertPositiveIntValue(Object target, string propertyPath)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyPath);
            Assert.IsNotNull(property, $"{target.name}.{propertyPath} is missing.");
            Assert.Greater(property.intValue, 0, $"{target.name}.{propertyPath} must be greater than zero.");
        }

        private static void AssertPrefabReferencePath(Object target, string propertyPath, string expectedPath)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyPath);
            Assert.IsNotNull(property, $"{target.name}.{propertyPath} is missing.");
            Assert.IsNotNull(property.objectReferenceValue, $"{target.name}.{propertyPath} is not wired.");
            Assert.AreEqual(expectedPath, AssetDatabase.GetAssetPath(property.objectReferenceValue));
        }

        private static void AssertPrefabInstanceRootPath(GameObject instance, string expectedPath)
        {
            GameObject prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(instance);
            Assert.IsNotNull(prefabRoot, $"{instance.name} must be a connected prefab instance.");

            Object source = PrefabUtility.GetCorrespondingObjectFromSource(prefabRoot);
            Assert.IsNotNull(source, $"{instance.name} must have a source prefab.");
            Assert.AreEqual(expectedPath, AssetDatabase.GetAssetPath(source));
        }

        private static void AssertNavigatorTableRoots(KitchenTableNavigator navigator)
        {
            Transform movingTablesRoot = (Transform)GetObjectReference(navigator, "movingTablesRoot");
            Transform ricePaintingTableRoot = (Transform)GetObjectReference(navigator, "ricePaintingTableRoot");
            Assert.GreaterOrEqual(movingTablesRoot.childCount, 4);

            bool riceRootIsDirectChild = false;
            for (int i = 0; i < movingTablesRoot.childCount; i++)
            {
                if (movingTablesRoot.GetChild(i) == ricePaintingTableRoot)
                {
                    riceRootIsDirectChild = true;
                    break;
                }
            }

            Assert.IsTrue(riceRootIsDirectChild, "Rice painting table root must be a direct child of MovingTablesRoot.");
        }

        private static void AssertDragPreviewPrefabHasRollSeaweedCover()
        {
            GameObject dragPreviewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DragPreviewPrefabPath);
            Assert.IsNotNull(dragPreviewPrefab, $"{DragPreviewPrefabPath} must exist.");

            KitchenRollSeaweedCover cover = dragPreviewPrefab.GetComponent<KitchenRollSeaweedCover>();
            Assert.IsNotNull(cover, "KitchenDragPreview.prefab must include KitchenRollSeaweedCover on the root.");
            Assert.IsNotNull(cover.CoverRenderer, "KitchenRollSeaweedCover.coverRenderer must be wired.");
            Assert.AreEqual("RollSeaweedCover", cover.CoverRenderer.name);
            Assert.IsFalse(cover.CoverRenderer.gameObject.activeSelf, "RollSeaweedCover child must be inactive by default.");
        }

        private static Object GetObjectReference(Object target, string propertyPath)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyPath);
            Assert.IsNotNull(property, $"{target.name}.{propertyPath} is missing.");
            Assert.IsNotNull(property.objectReferenceValue, $"{target.name}.{propertyPath} is not wired.");
            return property.objectReferenceValue;
        }
    }
}
