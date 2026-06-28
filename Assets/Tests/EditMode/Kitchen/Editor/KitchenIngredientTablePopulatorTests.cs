using System.Text;
using KimbapGame.Audio;
using KimbapGame.Kitchen;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace KimbapGame.Tests.Kitchen
{
    public sealed class KitchenIngredientTablePopulatorTests
    {
        private GameObject populatorObject;
        private GameObject seaweedSourcePrefabObject;
        private GameObject riceSourcePrefabObject;
        private GameObject fillingSourcePrefabObject;
        private GameObject legacySourcePrefabObject;
        private GameObject dragPrefabObject;
        private GameObject controllerObject;
        private GameObject dropZoneObject;
        private GameObject cameraObject;
        private GameObject seaweedRoot;
        private GameObject riceRoot;
        private GameObject fillingRoot;

        [SetUp]
        public void SetUp()
        {
            KimbapSfxPlayer.ResetDiagnosticsForTests();
        }

        [TearDown]
        public void TearDown()
        {
            Destroy(populatorObject);
            Destroy(seaweedSourcePrefabObject);
            Destroy(riceSourcePrefabObject);
            Destroy(fillingSourcePrefabObject);
            Destroy(legacySourcePrefabObject);
            Destroy(dragPrefabObject);
            Destroy(controllerObject);
            Destroy(dropZoneObject);
            Destroy(cameraObject);
            Destroy(seaweedRoot);
            Destroy(riceRoot);
            Destroy(fillingRoot);
            KimbapSfxPlayer.ResetDiagnosticsForTests();
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
            AssertHasChild(seaweedRoot.transform.GetChild(0), "SeaweedPrefabMarker");
            AssertHasChild(riceRoot.transform.GetChild(0), "RicePrefabMarker");
            AssertHasChild(fillingRoot.transform.GetChild(0), "FillingPrefabMarker");

            KitchenIngredientSource fillingSource = fillingRoot.transform.GetChild(0).GetComponent<KitchenIngredientSource>();
            AssertObjectReference(fillingSource, "controller");
            AssertObjectReference(fillingSource, "dropZone");
            AssertObjectReference(fillingSource, "targetCamera");
            AssertObjectReference(fillingSource, "definition.dragPrefab");
            Assert.AreEqual("r31", GetStringValue(fillingSource, "definition.variantId"));
        }

        [Test]
        public void PopulateFromCsv_AppliesTableSpecificScaleDragSizeAndColor()
        {
            KitchenIngredientTablePopulator populator = CreatePopulator();

            populator.PopulateFromCsv(BuildMixedCsv(seaweedCount: 1, riceCount: 1, fillingCount: 1));

            KitchenIngredientSource seaweedSource = seaweedRoot.transform.GetChild(0).GetComponent<KitchenIngredientSource>();
            KitchenIngredientSource riceSource = riceRoot.transform.GetChild(0).GetComponent<KitchenIngredientSource>();
            KitchenIngredientSource fillingSource = fillingRoot.transform.GetChild(0).GetComponent<KitchenIngredientSource>();
            AssertLocalScale(new Vector3(1.1f, 1.2f, 1f), seaweedSource.transform);
            AssertLocalScale(new Vector3(0.8f, 0.7f, 1f), riceSource.transform);
            AssertLocalScale(new Vector3(1.3f, 0.6f, 1f), fillingSource.transform);
            AssertVector2(new Vector2(2.4f, 1.7f), GetVector2Value(seaweedSource, "dragPreviewSize"));
            AssertVector2(new Vector2(2.8f, 0.28f), GetVector2Value(riceSource, "dragPreviewSize"));
            AssertVector2(new Vector2(2.1f, 0.4f), GetVector2Value(fillingSource, "dragPreviewSize"));
            AssertColor(new Color(0.08f, 0.16f, 0.11f, 1f), GetColorValue(seaweedSource, "definition.placeholderColor"));
            AssertColor(new Color(1f, 0.97f, 0.86f, 1f), GetColorValue(riceSource, "definition.placeholderColor"));
            AssertColor(new Color(0.95f, 0.44f, 0.42f, 1f), GetColorValue(fillingSource, "definition.placeholderColor"));
        }

        [Test]
        public void PopulateFromCsv_OrdersLaterSourcesAboveEarlierSources()
        {
            KitchenIngredientTablePopulator populator = CreatePopulator();

            populator.PopulateFromCsv(BuildSeaweedCsv(3));

            Transform first = seaweedRoot.transform.GetChild(0);
            Transform second = seaweedRoot.transform.GetChild(1);
            Transform third = seaweedRoot.transform.GetChild(2);
            Assert.AreEqual(4, MinSortingOrder(first));
            Assert.AreEqual(5, MaxSortingOrder(first));
            Assert.AreEqual(14, MinSortingOrder(second));
            Assert.AreEqual(15, MaxSortingOrder(second));
            Assert.AreEqual(24, MinSortingOrder(third));
            Assert.AreEqual(25, MaxSortingOrder(third));
            Assert.Greater(MinSortingOrder(third), MaxSortingOrder(second));
            Assert.Greater(MinSortingOrder(second), MaxSortingOrder(first));
        }

        [Test]
        public void PopulateFromCsv_AppliesTableSpecificSortingSettings()
        {
            KitchenIngredientTablePopulator populator = CreatePopulator();

            populator.PopulateFromCsv(BuildMixedCsv(seaweedCount: 2, riceCount: 2, fillingCount: 2));

            Assert.AreEqual(4, MinSortingOrder(seaweedRoot.transform.GetChild(0)));
            Assert.AreEqual(14, MinSortingOrder(seaweedRoot.transform.GetChild(1)));
            Assert.AreEqual(50, MinSortingOrder(riceRoot.transform.GetChild(0)));
            Assert.AreEqual(55, MinSortingOrder(riceRoot.transform.GetChild(1)));
            Assert.AreEqual(90, MinSortingOrder(fillingRoot.transform.GetChild(0)));
            Assert.AreEqual(93, MinSortingOrder(fillingRoot.transform.GetChild(1)));
        }

        [Test]
        public void PopulateFromCsv_LegacyTopLevelFieldsFallbackIntoSettings()
        {
            KitchenIngredientTablePopulator populator = CreatePopulator(useTableSettings: false);

            populator.PopulateFromCsv(BuildMixedCsv(seaweedCount: 1, riceCount: 1, fillingCount: 1));

            Assert.AreEqual(3, populator.SpawnedSources.Count);
            Assert.AreEqual(1, seaweedRoot.transform.childCount);
            Assert.AreEqual(1, riceRoot.transform.childCount);
            Assert.AreEqual(1, fillingRoot.transform.childCount);
            AssertHasChild(seaweedRoot.transform.GetChild(0), "LegacyPrefabMarker");
            AssertHasChild(riceRoot.transform.GetChild(0), "LegacyPrefabMarker");
            AssertHasChild(fillingRoot.transform.GetChild(0), "LegacyPrefabMarker");
            AssertLocalPosition(new Vector3(0f, 3f, -0.2f), seaweedRoot.transform.GetChild(0));
            AssertVector2(new Vector2(2.4f, 1.7f), GetVector2Value(seaweedRoot.transform.GetChild(0).GetComponent<KitchenIngredientSource>(), "dragPreviewSize"));
            AssertVector2(new Vector2(2.8f, 0.28f), GetVector2Value(riceRoot.transform.GetChild(0).GetComponent<KitchenIngredientSource>(), "dragPreviewSize"));
            AssertVector2(new Vector2(2.1f, 0.4f), GetVector2Value(fillingRoot.transform.GetChild(0).GetComponent<KitchenIngredientSource>(), "dragPreviewSize"));
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

            AssertLocalPosition(new Vector3(0f, 2.575f, -0.2f), seaweedRoot.transform.GetChild(10));
        }

        [Test]
        public void PopulateFromCsv_WithTwoItemsOnSecondRow_CentersPartialRow()
        {
            KitchenIngredientTablePopulator populator = CreatePopulator();

            populator.PopulateFromCsv(BuildSeaweedCsv(12));

            AssertLocalPosition(new Vector3(-0.55f, 2.575f, -0.2f), seaweedRoot.transform.GetChild(10));
            AssertLocalPosition(new Vector3(0.55f, 2.575f, -0.2f), seaweedRoot.transform.GetChild(11));
        }

        [Test]
        public void PopulateFromCsv_UsesCategorySpecificColumnLimits()
        {
            KitchenIngredientTablePopulator populator = CreatePopulator(
                seaweedItemsPerRow: 3,
                riceItemsPerRow: 2,
                fillingItemsPerRow: 4);

            populator.PopulateFromCsv(BuildMixedCsv(seaweedCount: 5, riceCount: 5, fillingCount: 5));

            AssertLocalPosition(new Vector3(-0.55f, 2.575f, -0.2f), seaweedRoot.transform.GetChild(3));
            AssertLocalPosition(new Vector3(0f, 2.15f, -0.2f), riceRoot.transform.GetChild(4));
            AssertLocalPosition(new Vector3(0f, 2.575f, -0.2f), fillingRoot.transform.GetChild(4));
        }

        [Test]
        public void PopulateFromCsv_ClampsCategoryColumnLimitToAtLeastOne()
        {
            KitchenIngredientTablePopulator populator = CreatePopulator(seaweedItemsPerRow: -2);

            populator.PopulateFromCsv(BuildSeaweedCsv(2));

            AssertLocalPosition(new Vector3(0f, 3.425f, -0.2f), seaweedRoot.transform.GetChild(0));
            AssertLocalPosition(new Vector3(0f, 2.575f, -0.2f), seaweedRoot.transform.GetChild(1));
        }

        [Test]
        public void PopulateFromCsv_AppliesVisualSpriteToSourceRenderer()
        {
            KitchenIngredientTablePopulator populator = CreatePopulator();
            Sprite expectedSprite = KitchenIngredientSpriteLoader.Load("햄_box_0", "햄", "r31");

            populator.PopulateFromCsv(
                "Index,이름,분류,가격,이미지,필수여부,브러시ID,브러시이미지\n"
                + "r31,햄,속,,햄_box_0,,b31,햄_line_0\n");

            SpriteRenderer renderer = fillingRoot.transform.GetChild(0).GetComponentInChildren<SpriteRenderer>();
            Assert.IsNotNull(renderer);
            Assert.AreSame(expectedSprite, renderer.sprite);
            AssertColor(Color.white, renderer.color);
        }

        [Test]
        public void PopulateFromCsv_AlignsPickupColliderToIngredientRendererBounds()
        {
            KitchenIngredientTablePopulator populator = CreatePopulator();
            Transform prefabIngredient = fillingSourcePrefabObject.transform.Find("FillingPrefabMarker");
            prefabIngredient.localPosition = new Vector3(0.35f, -0.2f, 0f);
            prefabIngredient.localScale = new Vector3(1.6f, 0.45f, 1f);

            populator.PopulateFromCsv(
                "Index,이름,분류,가격,이미지,필수여부\n"
                + "f1,floor,속,,Kitchen/floor,\n");

            Transform sourceRoot = fillingRoot.transform.GetChild(0);
            var pickupCollider = sourceRoot.GetComponent<BoxCollider2D>();
            SpriteRenderer ingredientRenderer = sourceRoot.Find("FillingPrefabMarker").GetComponent<SpriteRenderer>();

            Assert.IsNotNull(pickupCollider);
            Assert.IsNotNull(ingredientRenderer);
            AssertColliderMatchesRendererBounds(pickupCollider, ingredientRenderer, sourceRoot);
        }

        [Test]
        public void SourceClick_AppliesBrushImageSpriteToFillingDragPreviewRenderer()
        {
            KitchenIngredientTablePopulator populator = CreatePopulator();
            Sprite expectedSprite = KitchenIngredientSpriteLoader.Load("햄_line_0", "햄", "r31");

            populator.PopulateFromCsv(
                "Index,이름,분류,가격,이미지,필수여부,브러시ID,브러시이미지\n"
                + "r31,햄,속,,햄_box_0,,b31,햄_line_0\n");

            KitchenIngredientSource source = fillingRoot.transform.GetChild(0).GetComponent<KitchenIngredientSource>();
            source.SendMessage("OnMouseDown");
            KitchenDraggableItem preview = FindSpawnedPreview();

            try
            {
                Assert.IsNotNull(preview);
                Assert.AreEqual("Kitchen/Sound/재료 픽_mastered", KimbapSfxPlayer.LastRequestedResourcePath);
                SpriteRenderer renderer = preview.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(renderer);
                Assert.AreSame(expectedSprite, renderer.sprite);
                AssertColor(Color.white, renderer.color);
            }
            finally
            {
                if (preview != null)
                {
                    Destroy(preview.gameObject);
                }
            }
        }

        [Test]
        public void SourceClick_ForSeaweedFallsBackToVisualSprite()
        {
            KitchenIngredientTablePopulator populator = CreatePopulator();
            Sprite expectedSprite = KitchenIngredientSpriteLoader.Load("Kitchen/floor", "기본 김", "r1");

            populator.PopulateFromCsv(
                "Index,이름,분류,가격,이미지,필수여부,브러시ID,브러시이미지\n"
                + "r1,기본 김,김,,Kitchen/floor,,b1,김_line_0\n");

            KitchenIngredientSource source = seaweedRoot.transform.GetChild(0).GetComponent<KitchenIngredientSource>();
            source.SendMessage("OnMouseDown");
            KitchenDraggableItem preview = FindSpawnedPreview();

            try
            {
                Assert.IsNotNull(preview);
                Assert.AreEqual("Kitchen/Sound/김 집기", KimbapSfxPlayer.LastRequestedResourcePath);
                SpriteRenderer renderer = preview.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(renderer);
                Assert.AreSame(expectedSprite, renderer.sprite);
                AssertColor(Color.white, renderer.color);
            }
            finally
            {
                if (preview != null)
                {
                    Destroy(preview.gameObject);
                }
            }
        }

        [Test]
        public void SourceClick_SpawnsDragPreviewAtIngredientRendererCenter()
        {
            KitchenIngredientTablePopulator populator = CreatePopulator();
            Transform prefabIngredient = fillingSourcePrefabObject.transform.Find("FillingPrefabMarker");
            prefabIngredient.localPosition = new Vector3(-0.4f, 0.25f, 0f);

            populator.PopulateFromCsv(
                "Index,이름,분류,가격,이미지,필수여부\n"
                + "f1,floor,속,,Kitchen/floor,\n");

            KitchenIngredientSource source = fillingRoot.transform.GetChild(0).GetComponent<KitchenIngredientSource>();
            SpriteRenderer ingredientRenderer = fillingRoot.transform.GetChild(0).Find("FillingPrefabMarker").GetComponent<SpriteRenderer>();
            Vector3 expectedPosition = ingredientRenderer.bounds.center;
            expectedPosition.z = source.transform.position.z - 1f;

            source.SendMessage("OnMouseDown");
            KitchenDraggableItem preview = FindSpawnedPreview();

            try
            {
                Assert.IsNotNull(preview);
                AssertVector3(expectedPosition, preview.transform.position);
            }
            finally
            {
                if (preview != null)
                {
                    Destroy(preview.gameObject);
                }
            }
        }

        [Test]
        public void SourceClick_ForRiceDoesNotSpawnDragPreview()
        {
            KitchenIngredientTablePopulator populator = CreatePopulator();

            populator.PopulateFromCsv(
                "Index,이름,분류,가격,이미지,필수여부\n"
                + "r1,흰쌀밥,밥,,,\n");

            KitchenIngredientSource source = riceRoot.transform.GetChild(0).GetComponent<KitchenIngredientSource>();

            source.SendMessage("OnMouseDown");

            Assert.IsNull(FindSpawnedPreview());
            Assert.AreEqual("Kitchen/Sound/밥 선택_mastered", KimbapSfxPlayer.LastRequestedResourcePath);
        }

        private KitchenIngredientTablePopulator CreatePopulator(
            int seaweedItemsPerRow = 10,
            int riceItemsPerRow = 10,
            int fillingItemsPerRow = 10,
            bool useTableSettings = true)
        {
            populatorObject = new GameObject("KitchenIngredientTablePopulator");
            dragPrefabObject = new GameObject("KitchenDragPreview");
            controllerObject = new GameObject("KitchenController");
            dropZoneObject = new GameObject("KitchenDropZone");
            cameraObject = new GameObject("Main Camera");
            seaweedRoot = new GameObject("Seaweed Table");
            riceRoot = new GameObject("Rice Table");
            fillingRoot = new GameObject("Filling Table");

            KitchenIngredientSource seaweedSourcePrefab = CreateSourcePrefab("KitchenSeaweedIngredientSourcePrefab", "SeaweedPrefabMarker", out seaweedSourcePrefabObject);
            KitchenIngredientSource riceSourcePrefab = CreateSourcePrefab("KitchenRiceIngredientSourcePrefab", "RicePrefabMarker", out riceSourcePrefabObject);
            KitchenIngredientSource fillingSourcePrefab = CreateSourcePrefab("KitchenFillingIngredientSourcePrefab", "FillingPrefabMarker", out fillingSourcePrefabObject);
            KitchenIngredientSource legacySourcePrefab = CreateSourcePrefab("KitchenLegacyIngredientSourcePrefab", "LegacyPrefabMarker", out legacySourcePrefabObject);
            dragPrefabObject.AddComponent<SpriteRenderer>();
            dragPrefabObject.AddComponent<KitchenDraggableItem>();
            KitchenController controller = controllerObject.AddComponent<KitchenController>();
            KitchenDropZone dropZone = dropZoneObject.AddComponent<KitchenDropZone>();
            Camera camera = cameraObject.AddComponent<Camera>();
            KitchenIngredientTablePopulator populator = populatorObject.AddComponent<KitchenIngredientTablePopulator>();

            SerializedObject serializedObject = new SerializedObject(populator);
            serializedObject.FindProperty("dragPreviewPrefab").objectReferenceValue = dragPrefabObject;
            serializedObject.FindProperty("controller").objectReferenceValue = controller;
            serializedObject.FindProperty("dropZone").objectReferenceValue = dropZone;
            serializedObject.FindProperty("targetCamera").objectReferenceValue = camera;
            if (useTableSettings)
            {
                SetTableSettings(
                    serializedObject,
                    "seaweedSettings",
                    seaweedRoot.transform,
                    seaweedSourcePrefab,
                    new Vector3(0f, 3f, -0.2f),
                    new Vector2(1.1f, -0.85f),
                    seaweedItemsPerRow,
                    new Vector3(1.1f, 1.2f, 1f),
                    new Vector2(2.4f, 1.7f),
                    new Color(0.08f, 0.16f, 0.11f, 1f),
                    4,
                    10);
                SetTableSettings(
                    serializedObject,
                    "riceSettings",
                    riceRoot.transform,
                    riceSourcePrefab,
                    new Vector3(0f, 3f, -0.2f),
                    new Vector2(1.1f, -0.85f),
                    riceItemsPerRow,
                    new Vector3(0.8f, 0.7f, 1f),
                    new Vector2(2.8f, 0.28f),
                    new Color(1f, 0.97f, 0.86f, 1f),
                    50,
                    5);
                SetTableSettings(
                    serializedObject,
                    "fillingSettings",
                    fillingRoot.transform,
                    fillingSourcePrefab,
                    new Vector3(0f, 3f, -0.2f),
                    new Vector2(1.1f, -0.85f),
                    fillingItemsPerRow,
                    new Vector3(1.3f, 0.6f, 1f),
                    new Vector2(2.1f, 0.4f),
                    new Color(0.95f, 0.44f, 0.42f, 1f),
                    90,
                    3);
            }
            else
            {
                serializedObject.FindProperty("sourcePrefab").objectReferenceValue = legacySourcePrefab;
                serializedObject.FindProperty("seaweedTableRoot").objectReferenceValue = seaweedRoot.transform;
                serializedObject.FindProperty("riceTableRoot").objectReferenceValue = riceRoot.transform;
                serializedObject.FindProperty("fillingTableRoot").objectReferenceValue = fillingRoot.transform;
                serializedObject.FindProperty("sourceRowCenter").vector3Value = new Vector3(0f, 3f, -0.2f);
                serializedObject.FindProperty("sourceSpacing").vector2Value = new Vector2(1.1f, -0.85f);
                serializedObject.FindProperty("seaweedItemsPerRow").intValue = seaweedItemsPerRow;
                serializedObject.FindProperty("riceItemsPerRow").intValue = riceItemsPerRow;
                serializedObject.FindProperty("fillingItemsPerRow").intValue = fillingItemsPerRow;
                serializedObject.FindProperty("sourceLocalScale").vector3Value = Vector3.one;
                serializedObject.FindProperty("seaweedDragPreviewSize").vector2Value = new Vector2(2.4f, 1.7f);
                serializedObject.FindProperty("riceDragPreviewSize").vector2Value = new Vector2(2.8f, 0.28f);
                serializedObject.FindProperty("fillingDragPreviewSize").vector2Value = new Vector2(2.1f, 0.4f);
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            return populator;
        }

        private static KitchenIngredientSource CreateSourcePrefab(string objectName, string markerName, out GameObject prefabObject)
        {
            prefabObject = new GameObject(objectName);
            KitchenIngredientSource sourcePrefab = prefabObject.AddComponent<KitchenIngredientSource>();
            prefabObject.AddComponent<BoxCollider2D>();
            GameObject ingredientObject = new GameObject(markerName);
            ingredientObject.transform.SetParent(prefabObject.transform);
            SpriteRenderer ingredientRenderer = ingredientObject.AddComponent<SpriteRenderer>();
            ingredientRenderer.sortingOrder = 5;
            GameObject lowerRendererObject = new GameObject("LowerSortingMarker");
            lowerRendererObject.transform.SetParent(prefabObject.transform);
            SpriteRenderer lowerRenderer = lowerRendererObject.AddComponent<SpriteRenderer>();
            lowerRenderer.sortingOrder = 4;

            SerializedObject sourceSerializedObject = new SerializedObject(sourcePrefab);
            sourceSerializedObject.FindProperty("ingredientRenderer").objectReferenceValue = ingredientRenderer;
            sourceSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            return sourcePrefab;
        }

        private static void SetTableSettings(
            SerializedObject serializedObject,
            string propertyPath,
            Transform tableRoot,
            KitchenIngredientSource sourcePrefab,
            Vector3 rowCenter,
            Vector2 spacing,
            int itemsPerRow,
            Vector3 localScale,
            Vector2 dragPreviewSize,
            Color placeholderColor,
            int sourceSortingOrderBase,
            int sourceSortingOrderStep)
        {
            SerializedProperty settings = serializedObject.FindProperty(propertyPath);
            Assert.IsNotNull(settings, propertyPath);
            settings.FindPropertyRelative("tableRoot").objectReferenceValue = tableRoot;
            settings.FindPropertyRelative("sourcePrefab").objectReferenceValue = sourcePrefab;
            settings.FindPropertyRelative("rowCenter").vector3Value = rowCenter;
            settings.FindPropertyRelative("spacing").vector2Value = spacing;
            settings.FindPropertyRelative("itemsPerRow").intValue = itemsPerRow;
            settings.FindPropertyRelative("localScale").vector3Value = localScale;
            settings.FindPropertyRelative("dragPreviewSize").vector2Value = dragPreviewSize;
            settings.FindPropertyRelative("placeholderColor").colorValue = placeholderColor;
            settings.FindPropertyRelative("sourceSortingOrderBase").intValue = sourceSortingOrderBase;
            settings.FindPropertyRelative("sourceSortingOrderStep").intValue = sourceSortingOrderStep;
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

        private static string BuildMixedCsv(int seaweedCount, int riceCount, int fillingCount)
        {
            StringBuilder builder = new StringBuilder("Index,이름,분류,가격,이미지,필수여부\n");
            AppendRows(builder, "s", "김", "김", seaweedCount);
            AppendRows(builder, "r", "밥", "밥", riceCount);
            AppendRows(builder, "f", "속", "속", fillingCount);
            return builder.ToString();
        }

        private static void AppendRows(StringBuilder builder, string idPrefix, string namePrefix, string category, int count)
        {
            for (int i = 0; i < count; i++)
            {
                builder.Append(idPrefix);
                builder.Append(i);
                builder.Append(',');
                builder.Append(namePrefix);
                builder.Append(i);
                builder.Append(',');
                builder.Append(category);
                builder.Append(",,,\n");
            }
        }

        private static void AssertObjectReference(Object target, string propertyPath)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyPath);
            Assert.IsNotNull(property, $"{target.name}.{propertyPath} is missing.");
            Assert.IsNotNull(property.objectReferenceValue, $"{target.name}.{propertyPath} is not wired.");
        }

        private static void AssertHasChild(Transform parent, string childName)
        {
            Assert.IsNotNull(parent.Find(childName), $"{parent.name} should be spawned from a prefab with child '{childName}'.");
        }

        private static string GetStringValue(Object target, string propertyPath)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyPath);
            Assert.IsNotNull(property, $"{target.name}.{propertyPath} is missing.");
            return property.stringValue;
        }

        private static Vector2 GetVector2Value(Object target, string propertyPath)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyPath);
            Assert.IsNotNull(property, $"{target.name}.{propertyPath} is missing.");
            return property.vector2Value;
        }

        private static Color GetColorValue(Object target, string propertyPath)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyPath);
            Assert.IsNotNull(property, $"{target.name}.{propertyPath} is missing.");
            return property.colorValue;
        }

        private static void AssertLocalPosition(Vector3 expected, Transform actual)
        {
            Assert.AreEqual(expected.x, actual.localPosition.x, 0.0001f);
            Assert.AreEqual(expected.y, actual.localPosition.y, 0.0001f);
            Assert.AreEqual(expected.z, actual.localPosition.z, 0.0001f);
        }

        private static void AssertLocalScale(Vector3 expected, Transform actual)
        {
            Assert.AreEqual(expected.x, actual.localScale.x, 0.0001f);
            Assert.AreEqual(expected.y, actual.localScale.y, 0.0001f);
            Assert.AreEqual(expected.z, actual.localScale.z, 0.0001f);
        }

        private static void AssertVector2(Vector2 expected, Vector2 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 0.0001f);
            Assert.AreEqual(expected.y, actual.y, 0.0001f);
        }

        private static void AssertVector3(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 0.0001f);
            Assert.AreEqual(expected.y, actual.y, 0.0001f);
            Assert.AreEqual(expected.z, actual.z, 0.0001f);
        }

        private static void AssertColliderMatchesRendererBounds(
            BoxCollider2D pickupCollider,
            SpriteRenderer renderer,
            Transform sourceRoot)
        {
            Bounds localBounds = GetRendererBoundsInRootLocal(renderer, sourceRoot);
            Assert.AreEqual(localBounds.center.x, pickupCollider.offset.x, 0.0001f);
            Assert.AreEqual(localBounds.center.y, pickupCollider.offset.y, 0.0001f);
            Assert.AreEqual(localBounds.size.x, pickupCollider.size.x, 0.0001f);
            Assert.AreEqual(localBounds.size.y, pickupCollider.size.y, 0.0001f);
        }

        private static Bounds GetRendererBoundsInRootLocal(SpriteRenderer renderer, Transform sourceRoot)
        {
            Bounds spriteBounds = renderer.sprite.bounds;
            Vector3 min = spriteBounds.min;
            Vector3 max = spriteBounds.max;
            Vector3 firstPoint = ToRootLocal(renderer.transform, sourceRoot, new Vector3(min.x, min.y, 0f));
            Bounds bounds = new Bounds(firstPoint, Vector3.zero);
            bounds.Encapsulate(ToRootLocal(renderer.transform, sourceRoot, new Vector3(min.x, max.y, 0f)));
            bounds.Encapsulate(ToRootLocal(renderer.transform, sourceRoot, new Vector3(max.x, min.y, 0f)));
            bounds.Encapsulate(ToRootLocal(renderer.transform, sourceRoot, new Vector3(max.x, max.y, 0f)));
            return bounds;
        }

        private static Vector3 ToRootLocal(Transform rendererTransform, Transform sourceRoot, Vector3 rendererLocalPoint)
        {
            return sourceRoot.InverseTransformPoint(rendererTransform.TransformPoint(rendererLocalPoint));
        }

        private static int MinSortingOrder(Transform root)
        {
            SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>();
            Assert.Greater(renderers.Length, 0);
            int min = renderers[0].sortingOrder;
            for (int i = 1; i < renderers.Length; i++)
            {
                min = Mathf.Min(min, renderers[i].sortingOrder);
            }

            return min;
        }

        private static int MaxSortingOrder(Transform root)
        {
            SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>();
            Assert.Greater(renderers.Length, 0);
            int max = renderers[0].sortingOrder;
            for (int i = 1; i < renderers.Length; i++)
            {
                max = Mathf.Max(max, renderers[i].sortingOrder);
            }

            return max;
        }

        private KitchenDraggableItem FindSpawnedPreview()
        {
            KitchenDraggableItem[] previews = Object.FindObjectsByType<KitchenDraggableItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < previews.Length; i++)
            {
                if (previews[i].gameObject != dragPrefabObject)
                {
                    return previews[i];
                }
            }

            return null;
        }

        private static void AssertColor(Color expected, Color actual)
        {
            Assert.AreEqual(expected.r, actual.r, 0.0001f);
            Assert.AreEqual(expected.g, actual.g, 0.0001f);
            Assert.AreEqual(expected.b, actual.b, 0.0001f);
            Assert.AreEqual(expected.a, actual.a, 0.0001f);
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
