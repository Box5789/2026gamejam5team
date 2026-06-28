using KimbapGame.Kitchen;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace KimbapGame.Tests.Kitchen
{
    public sealed class KitchenTableNavigatorTests
    {
        private readonly System.Collections.Generic.List<GameObject> createdObjects = new System.Collections.Generic.List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null)
                {
                    Object.DestroyImmediate(createdObjects[i]);
                }
            }

            createdObjects.Clear();
        }

        [TestCase(-1, 3, 0)]
        [TestCase(0, 3, 0)]
        [TestCase(1, 3, 1)]
        [TestCase(2, 3, 2)]
        [TestCase(3, 3, 2)]
        [TestCase(99, 3, 2)]
        [TestCase(3, 4, 3)]
        [TestCase(4, 4, 3)]
        [TestCase(99, 4, 3)]
        [TestCase(2, 0, 0)]
        public void ClampTableIndex_StaysInsideTableRange(int input, int tableCount, int expected)
        {
            Assert.AreEqual(expected, KitchenTableNavigator.ClampTableIndex(input, tableCount));
        }

        [Test]
        public void SetTableIndex_UsesChildPositionsForUniformSpacing()
        {
            Transform movingRoot = CreateRoot(new Vector3(3f, 2f, 1f));
            CreateTable(movingRoot, "Seaweed Table", new Vector3(0f, 0f, 0f));
            CreateTable(movingRoot, "Rice Table", new Vector3(9f, 0f, 0f));
            CreateTable(movingRoot, "Filling Table", new Vector3(18f, 0f, 0f));
            CreateTable(movingRoot, "Complete Table", new Vector3(27f, 0f, 0f));
            KitchenTableNavigator navigator = CreateNavigator(movingRoot);

            navigator.SetTableIndex(2);

            AssertVector(new Vector3(-15f, 2f, 1f), movingRoot.localPosition);
            Assert.AreEqual(4, navigator.TableCount);
        }

        [Test]
        public void SetTableIndex_UsesIrregularChildSpacing()
        {
            Transform movingRoot = CreateRoot(new Vector3(1f, -2f, 4f));
            CreateTable(movingRoot, "Seaweed Table", new Vector3(0f, 0f, 0f));
            CreateTable(movingRoot, "Rice Table", new Vector3(6.5f, 0f, 0f));
            CreateTable(movingRoot, "Filling Table", new Vector3(15f, 0f, 0f));
            KitchenTableNavigator navigator = CreateNavigator(movingRoot);

            navigator.SetTableIndex(2);

            AssertVector(new Vector3(-14f, -2f, 4f), movingRoot.localPosition);
        }

        [Test]
        public void SetTableIndex_UsesDeltaFromFirstChildWhenFirstTableIsOffset()
        {
            Transform movingRoot = CreateRoot(new Vector3(0f, 0f, 2f));
            CreateTable(movingRoot, "Seaweed Table", new Vector3(5f, 2f, 0f));
            CreateTable(movingRoot, "Rice Table", new Vector3(12f, -1f, 100f));
            KitchenTableNavigator navigator = CreateNavigator(movingRoot);

            navigator.SetTableIndex(1);

            AssertVector(new Vector3(-7f, 3f, 2f), movingRoot.localPosition);
        }

        [Test]
        public void SetTableIndex_ClampsAgainstMovingRootChildCount()
        {
            Transform movingRoot = CreateRoot(Vector3.zero);
            CreateTable(movingRoot, "Seaweed Table", Vector3.zero);
            CreateTable(movingRoot, "Rice Table", new Vector3(10f, 0f, 0f));
            CreateTable(movingRoot, "Filling Table", new Vector3(20f, 0f, 0f));
            KitchenTableNavigator navigator = CreateNavigator(movingRoot);

            navigator.SetTableIndex(99);
            Assert.AreEqual(2, navigator.CurrentTableIndex);

            navigator.SetTableIndex(-1);
            Assert.AreEqual(0, navigator.CurrentTableIndex);
        }

        [Test]
        public void SetTableIndex_EnablesRicePaintingOnlyForConfiguredRiceTableRoot()
        {
            Transform movingRoot = CreateRoot(Vector3.zero);
            CreateTable(movingRoot, "Seaweed Table", Vector3.zero);
            Transform riceTable = CreateTable(movingRoot, "Rice Table", new Vector3(10f, 0f, 0f));
            CreateTable(movingRoot, "Filling Table", new Vector3(20f, 0f, 0f));
            KitchenTableNavigator navigator = CreateNavigator(movingRoot);
            KitchenRicePaintBridge bridge = CreateObject("KitchenRicePaintBridge").AddComponent<KitchenRicePaintBridge>();
            SetObjectReference(navigator, "ricePaintBridge", bridge);
            SetObjectReference(navigator, "ricePaintingTableRoot", riceTable);

            Assert.IsFalse(bridge.PaintingEnabled);

            navigator.SetTableIndex(1);
            Assert.IsTrue(bridge.PaintingEnabled);

            navigator.SetTableIndex(2);
            Assert.IsFalse(bridge.PaintingEnabled);
        }

        private KitchenTableNavigator CreateNavigator(Transform movingRoot)
        {
            KitchenTableNavigator navigator = CreateObject("KitchenTableNavigator").AddComponent<KitchenTableNavigator>();
            navigator.Configure(movingRoot, null);
            SetFloat(navigator, "slideDuration", 0f);
            return navigator;
        }

        private Transform CreateRoot(Vector3 localPosition)
        {
            Transform root = CreateObject("MovingTablesRoot").transform;
            root.localPosition = localPosition;
            return root;
        }

        private Transform CreateTable(Transform parent, string name, Vector3 localPosition)
        {
            Transform table = CreateObject(name).transform;
            table.SetParent(parent, false);
            table.localPosition = localPosition;
            return table;
        }

        private GameObject CreateObject(string name)
        {
            GameObject gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static void SetObjectReference(Object target, string propertyPath, Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyPath).objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string propertyPath, float value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyPath).floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssertVector(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 0.0001f);
            Assert.AreEqual(expected.y, actual.y, 0.0001f);
            Assert.AreEqual(expected.z, actual.z, 0.0001f);
        }
    }
}
