using System.Text;
using KimbapGame.Kitchen;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace KimbapGame.Tests.Kitchen
{
    public sealed class KitchenIngredientTablePopulatorTests
    {
        private GameObject populatorObject;
        private GameObject sourcePrefabObject;
        private GameObject dragPrefabObject;
        private GameObject controllerObject;
        private GameObject dropZoneObject;
        private GameObject cameraObject;
        private GameObject seaweedRoot;
        private GameObject riceRoot;
        private GameObject fillingRoot;

        [TearDown]
        public void TearDown()
        {
            Destroy(populatorObject);
            Destroy(sourcePrefabObject);
            Destroy(dragPrefabObject);
            Destroy(controllerObject);
            Destroy(dropZoneObject);
            Destroy(cameraObject);
            Destroy(seaweedRoot);
            Destroy(riceRoot);
            Destroy(fillingRoot);
        }

        [Test]
        public void PopulateFromCsv_SpawnsSourceCategoriesAndSkipsSauce()
        {
            KitchenIngredientTablePopulator populator = CreatePopulator();

            populator.PopulateFromCsv(
                "Index,이름,분류,가격,이미지,필수여부\n"
                + "r1,기본 김,김,,,\n"
                + "r16,흰쌀밥,밥,,,\n"
                + "r31,햄,속,,,\n"
                + "r86,마요네즈,소스,,,\n");

            Assert.AreEqual(3, populator.SpawnedSources.Count);
            Assert.AreEqual(1, seaweedRoot.transform.childCount);
            Assert.AreEqual(1, riceRoot.transform.childCount);
            Assert.AreEqual(1, fillingRoot.transform.childCount);

            KitchenIngredientSource fillingSource = fillingRoot.transform.GetChild(0).GetComponent<KitchenIngredientSource>();
            AssertObjectReference(fillingSource, "controller");
            AssertObjectReference(fillingSource, "dropZone");
            AssertObjectReference(fillingSource, "targetCamera");
            AssertObjectReference(fillingSource, "definition.dragPrefab");
            Assert.AreEqual("r31", GetStringValue(fillingSource, "definition.variantId"));
        }

        [Test]
        public void PopulateFromCsv_WithOneItem_CentersRow()
        {
            KitchenIngredientTablePopulator populator = CreatePopulator();

            populator.PopulateFromCsv(BuildSeaweedCsv(1));

            AssertLocalPosition(new Vector3(0f, 3f, -0.2f), seaweedRoot.transform.GetChild(0));
        }

        [Test]
        public void PopulateFromCsv_WithTwoItems_CentersRow()
        {
            KitchenIngredientTablePopulator populator = CreatePopulator();

            populator.PopulateFromCsv(BuildSeaweedCsv(2));

            AssertLocalPosition(new Vector3(-0.55f, 3f, -0.2f), seaweedRoot.transform.GetChild(0));
            AssertLocalPosition(new Vector3(0.55f, 3f, -0.2f), seaweedRoot.transform.GetChild(1));
        }

        [Test]
        public void PopulateFromCsv_WithFullRow_CentersRow()
        {
            KitchenIngredientTablePopulator populator = CreatePopulator();

            populator.PopulateFromCsv(BuildSeaweedCsv(10));

            AssertLocalPosition(new Vector3(-4.95f, 3f, -0.2f), seaweedRoot.transform.GetChild(0));
            AssertLocalPosition(new Vector3(4.95f, 3f, -0.2f), seaweedRoot.transform.GetChild(9));
        }

        [Test]
        public void PopulateFromCsv_WithOneItemOnSecondRow_CentersPartialRow()
        {
            KitchenIngredientTablePopulator populator = CreatePopulator();

            populator.PopulateFromCsv(BuildSeaweedCsv(11));

            AssertLocalPosition(new Vector3(0f, 2.15f, -0.2f), seaweedRoot.transform.GetChild(10));
        }

        [Test]
        public void PopulateFromCsv_WithTwoItemsOnSecondRow_CentersPartialRow()
        {
            KitchenIngredientTablePopulator populator = CreatePopulator();

            populator.PopulateFromCsv(BuildSeaweedCsv(12));

            AssertLocalPosition(new Vector3(-0.55f, 2.15f, -0.2f), seaweedRoot.transform.GetChild(10));
            AssertLocalPosition(new Vector3(0.55f, 2.15f, -0.2f), seaweedRoot.transform.GetChild(11));
        }

        private KitchenIngredientTablePopulator CreatePopulator()
        {
            populatorObject = new GameObject("KitchenIngredientTablePopulator");
            sourcePrefabObject = new GameObject("KitchenIngredientSourcePrefab");
            dragPrefabObject = new GameObject("KitchenDragPreview");
            controllerObject = new GameObject("KitchenController");
            dropZoneObject = new GameObject("KitchenDropZone");
            cameraObject = new GameObject("Main Camera");
            seaweedRoot = new GameObject("Seaweed Table");
            riceRoot = new GameObject("Rice Table");
            fillingRoot = new GameObject("Filling Table");

            KitchenIngredientSource sourcePrefab = sourcePrefabObject.AddComponent<KitchenIngredientSource>();
            sourcePrefabObject.AddComponent<BoxCollider2D>();
            KitchenController controller = controllerObject.AddComponent<KitchenController>();
            KitchenDropZone dropZone = dropZoneObject.AddComponent<KitchenDropZone>();
            Camera camera = cameraObject.AddComponent<Camera>();
            KitchenIngredientTablePopulator populator = populatorObject.AddComponent<KitchenIngredientTablePopulator>();

            SerializedObject serializedObject = new SerializedObject(populator);
            serializedObject.FindProperty("sourcePrefab").objectReferenceValue = sourcePrefab;
            serializedObject.FindProperty("dragPreviewPrefab").objectReferenceValue = dragPrefabObject;
            serializedObject.FindProperty("controller").objectReferenceValue = controller;
            serializedObject.FindProperty("dropZone").objectReferenceValue = dropZone;
            serializedObject.FindProperty("targetCamera").objectReferenceValue = camera;
            serializedObject.FindProperty("seaweedTableRoot").objectReferenceValue = seaweedRoot.transform;
            serializedObject.FindProperty("riceTableRoot").objectReferenceValue = riceRoot.transform;
            serializedObject.FindProperty("fillingTableRoot").objectReferenceValue = fillingRoot.transform;
            serializedObject.FindProperty("sourceRowCenter").vector3Value = new Vector3(0f, 3f, -0.2f);
            serializedObject.FindProperty("sourceSpacing").vector2Value = new Vector2(1.1f, -0.85f);
            serializedObject.FindProperty("itemsPerRow").intValue = 10;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            return populator;
        }

        private static string BuildSeaweedCsv(int count)
        {
            StringBuilder builder = new StringBuilder("Index,이름,분류,가격,이미지,필수여부\n");
            for (int i = 0; i < count; i++)
            {
                builder.Append("s");
                builder.Append(i);
                builder.Append(",김");
                builder.Append(i);
                builder.Append(",김,,,\n");
            }

            return builder.ToString();
        }

        private static void AssertObjectReference(Object target, string propertyPath)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyPath);
            Assert.IsNotNull(property, $"{target.name}.{propertyPath} is missing.");
            Assert.IsNotNull(property.objectReferenceValue, $"{target.name}.{propertyPath} is not wired.");
        }

        private static string GetStringValue(Object target, string propertyPath)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyPath);
            Assert.IsNotNull(property, $"{target.name}.{propertyPath} is missing.");
            return property.stringValue;
        }

        private static void AssertLocalPosition(Vector3 expected, Transform actual)
        {
            Assert.AreEqual(expected.x, actual.localPosition.x, 0.0001f);
            Assert.AreEqual(expected.y, actual.localPosition.y, 0.0001f);
            Assert.AreEqual(expected.z, actual.localPosition.z, 0.0001f);
        }

        private static void Destroy(Object target)
        {
            if (target != null)
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
