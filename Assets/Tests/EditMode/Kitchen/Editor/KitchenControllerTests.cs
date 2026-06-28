using GameJam.Gameplay.Spreading;
using KimbapGame.Data;
using KimbapGame.Kitchen;
using NUnit.Framework;
using UnityEngine;

namespace KimbapGame.Tests.Kitchen
{
    public sealed class KitchenControllerTests
    {
        private GameObject gameObject;
        private GameObject riceSurfacePrefab;
        private KitchenController controller;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("KitchenControllerTests");
            controller = gameObject.AddComponent<KitchenController>();
            controller.ConfigureLimitsForTests(1, 2, 1);
            riceSurfacePrefab = new GameObject("RiceSurfacePrefab");
            riceSurfacePrefab.AddComponent<SpriteRenderer>();
            riceSurfacePrefab.AddComponent<SpreadableSurface>();
            riceSurfacePrefab.AddComponent<SpreadInputController>();
            controller.ConfigureRiceSurfacePrefabForTests(riceSurfacePrefab);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gameObject);
            Object.DestroyImmediate(riceSurfacePrefab);
        }

        [Test]
        public void TryAddSeaweed_RespectsConfiguredLimit()
        {
            KitchenIngredientDefinition seaweed = CreateDefinition(KitchenIngredientCategory.Seaweed, IngredientType.Seaweed);

            Assert.IsTrue(controller.TryAddSeaweed(seaweed));
            Assert.IsFalse(controller.TryAddSeaweed(seaweed));
            Assert.AreEqual(1, controller.PreparedKimbap.seaweeds.Count);
        }

        [Test]
        public void TryAddRice_AllowsDuplicatesUntilConfiguredLimit()
        {
            GameObject seaweedObject = new GameObject("SeaweedObject");
            KitchenIngredientDefinition seaweed = CreateDefinition(KitchenIngredientCategory.Seaweed, IngredientType.Seaweed);
            KitchenIngredientDefinition rice = CreateDefinition(KitchenIngredientCategory.Rice, IngredientType.Rice);

            try
            {
                Assert.IsTrue(controller.TryAddSeaweed(seaweed));
                controller.RegisterDroppedObject(seaweed, seaweedObject);

                Assert.IsTrue(controller.TryAddRice(rice));
                Assert.IsTrue(controller.TryAddRice(rice));
                Assert.IsFalse(controller.TryAddRice(rice));
                Assert.AreEqual(2, controller.PreparedKimbap.riceItems.Count);
            }
            finally
            {
                Object.DestroyImmediate(seaweedObject);
            }
        }

        [Test]
        public void TryAddRice_RequiresTopSeaweedSurface()
        {
            KitchenIngredientDefinition rice = CreateDefinition(KitchenIngredientCategory.Rice, IngredientType.Rice);

            Assert.IsFalse(controller.TryAddRice(rice));
            Assert.AreEqual(0, controller.PreparedKimbap.riceItems.Count);
        }

        [Test]
        public void RegisterDroppedObject_ForSeaweedCreatesCurrentRiceSurface()
        {
            GameObject seaweedObject = new GameObject("SeaweedObject");
            seaweedObject.AddComponent<SpriteRenderer>().sprite = KitchenPlaceholderFactory.CreateWhiteSprite();
            KitchenIngredientDefinition seaweed = CreateDefinition(KitchenIngredientCategory.Seaweed, IngredientType.Seaweed);

            try
            {
                controller.RegisterDroppedObject(seaweed, seaweedObject);

                Assert.AreSame(seaweedObject, controller.TopSeaweedObject);
                Assert.IsNotNull(controller.CurrentRiceSurface);
                Assert.IsNotNull(controller.CurrentRiceInputController);
                Assert.IsTrue(controller.CurrentRiceSurface.gameObject.activeSelf);
                Assert.IsTrue(controller.CurrentRiceSurface.GetComponent<SpriteRenderer>().enabled);
                Assert.AreEqual(Color.clear, controller.CurrentRiceSurface.SurfaceColor);
                Assert.AreEqual("SeaweedObject", controller.DebugTopSeaweedName);
                Assert.AreEqual("SeaweedObject", controller.DebugLastRegisteredObjectName);
            }
            finally
            {
                Object.DestroyImmediate(seaweedObject);
            }
        }

        [Test]
        public void RegisterDroppedObject_ForSecondSeaweedRemovesPreviousRiceSurface()
        {
            controller.ConfigureLimitsForTests(2, 2, 1);
            GameObject firstSeaweedObject = new GameObject("FirstSeaweedObject");
            GameObject secondSeaweedObject = new GameObject("SecondSeaweedObject");
            firstSeaweedObject.AddComponent<SpriteRenderer>().sprite = KitchenPlaceholderFactory.CreateWhiteSprite();
            secondSeaweedObject.AddComponent<SpriteRenderer>().sprite = KitchenPlaceholderFactory.CreateWhiteSprite();
            Color firstColor = new Color(0.1f, 0.2f, 0.1f, 1f);
            Color secondColor = new Color(0.25f, 0.35f, 0.2f, 1f);
            KitchenIngredientDefinition firstSeaweed = CreateDefinition(KitchenIngredientCategory.Seaweed, IngredientType.Seaweed, firstColor);
            KitchenIngredientDefinition secondSeaweed = CreateDefinition(KitchenIngredientCategory.Seaweed, IngredientType.Seaweed, secondColor);

            try
            {
                controller.RegisterDroppedObject(firstSeaweed, firstSeaweedObject);
                GameObject firstSurfaceObject = controller.CurrentRiceSurface.gameObject;

                controller.RegisterDroppedObject(secondSeaweed, secondSeaweedObject);

                Assert.IsTrue(firstSurfaceObject == null);
                Assert.AreSame(secondSeaweedObject, controller.TopSeaweedObject);
                Assert.AreSame(secondSeaweedObject.transform, controller.CurrentRiceSurface.transform.parent);
                Assert.AreEqual(Color.clear, controller.CurrentRiceSurface.SurfaceColor);
            }
            finally
            {
                Object.DestroyImmediate(firstSeaweedObject);
                Object.DestroyImmediate(secondSeaweedObject);
            }
        }

        [Test]
        public void RegisterDroppedObject_AlignsRiceSurfaceToSeaweedRendererBounds()
        {
            GameObject seaweedObject = CreateSeaweedObject(
                "SeaweedObject",
                position: new Vector3(1f, 2f, 0f),
                scale: new Vector3(4f, 2f, 1f),
                spritePivot: new Vector2(0f, 0f));
            KitchenIngredientDefinition seaweed = CreateDefinition(KitchenIngredientCategory.Seaweed, IngredientType.Seaweed);

            try
            {
                controller.RegisterDroppedObject(seaweed, seaweedObject);
                controller.CurrentRiceSurface.InitializeSurface();

                Bounds seaweedBounds = seaweedObject.GetComponent<SpriteRenderer>().bounds;
                Bounds riceBounds = controller.CurrentRiceSurface.GetComponent<SpriteRenderer>().bounds;

                Assert.AreEqual(seaweedBounds.center.x, riceBounds.center.x, 0.001f);
                Assert.AreEqual(seaweedBounds.center.y, riceBounds.center.y, 0.001f);
                Assert.AreEqual(seaweedBounds.size.x * 0.92f, riceBounds.size.x, 0.001f);
                Assert.AreEqual(seaweedBounds.size.y * 0.78f, riceBounds.size.y, 0.001f);
            }
            finally
            {
                DestroySeaweedObject(seaweedObject);
            }
        }

        [Test]
        public void RegisterDroppedObject_TransparentRiceSurfaceStillPaintsAtSeaweedCenter()
        {
            GameObject seaweedObject = CreateSeaweedObject(
                "SeaweedObject",
                position: Vector3.zero,
                scale: new Vector3(3f, 2f, 1f),
                spritePivot: new Vector2(0.5f, 0.5f));
            KitchenIngredientDefinition seaweed = CreateDefinition(KitchenIngredientCategory.Seaweed, IngredientType.Seaweed);

            try
            {
                controller.RegisterDroppedObject(seaweed, seaweedObject);
                SpreadableSurface surface = controller.CurrentRiceSurface;
                surface.InitializeSurface();
                Assert.AreEqual(Color.clear, surface.SurfaceColor);
                Assert.AreEqual(0f, surface.Coverage, 0.0001f);

                bool painted = surface.PaintAtWorldPoint(seaweedObject.GetComponent<SpriteRenderer>().bounds.center);

                Assert.IsTrue(painted);
                Assert.Greater(surface.Coverage, 0f);
            }
            finally
            {
                DestroySeaweedObject(seaweedObject);
            }
        }

        [Test]
        public void SpreadableSurface_SetSurfaceColorStoresRuntimeSurfaceColor()
        {
            GameObject surfaceObject = new GameObject("SpreadableSurfaceTest");
            surfaceObject.AddComponent<SpriteRenderer>();
            SpreadableSurface surface = surfaceObject.AddComponent<SpreadableSurface>();
            Color targetColor = new Color(0.12f, 0.24f, 0.18f, 1f);

            try
            {
                surface.SetSurfaceColor(targetColor);

                Assert.AreEqual(targetColor, surface.SurfaceColor);
            }
            finally
            {
                Object.DestroyImmediate(surfaceObject);
            }
        }

        [Test]
        public void TryAddFilling_RespectsConfiguredLimit()
        {
            KitchenIngredientDefinition filling = CreateDefinition(KitchenIngredientCategory.Filling, IngredientType.Ham);

            Assert.IsTrue(controller.TryAddFilling(filling));
            Assert.IsFalse(controller.TryAddFilling(filling));
            Assert.AreEqual(1, controller.PreparedKimbap.fillings.Count);
        }

        [Test]
        public void RegisterDroppedObject_ForFillingTracksDroppedFillingObject()
        {
            GameObject fillingObject = new GameObject("FillingObject");
            KitchenIngredientDefinition filling = CreateDefinition(KitchenIngredientCategory.Filling, IngredientType.Ham);

            try
            {
                controller.RegisterDroppedObject(filling, fillingObject);

                Assert.AreEqual(1, controller.DroppedFillingObjects.Count);
                Assert.AreSame(fillingObject, controller.DroppedFillingObjects[0]);
                Assert.AreEqual(1, controller.DebugDroppedFillingCount);
                Assert.AreEqual("FillingObject", controller.DebugLastRegisteredObjectName);
            }
            finally
            {
                Object.DestroyImmediate(fillingObject);
            }
        }

        [Test]
        public void ResetPreparation_ClearsDroppedFillingObjects()
        {
            GameObject fillingObject = new GameObject("FillingObject");
            KitchenIngredientDefinition filling = CreateDefinition(KitchenIngredientCategory.Filling, IngredientType.Ham);

            try
            {
                controller.RegisterDroppedObject(filling, fillingObject);

                controller.ResetPreparation();

                Assert.AreEqual(0, controller.DroppedFillingObjects.Count);
                Assert.AreEqual(0, controller.DebugDroppedFillingCount);
                Assert.AreEqual(string.Empty, controller.DebugLastRegisteredObjectName);
                Assert.AreEqual(string.Empty, controller.DebugTopSeaweedName);
            }
            finally
            {
                Object.DestroyImmediate(fillingObject);
            }
        }

        [Test]
        public void RicePaintBridge_DisablingPaintingClearsSelectedRice()
        {
            GameObject bridgeObject = new GameObject("KitchenRicePaintBridge");
            KitchenRicePaintBridge bridge = bridgeObject.AddComponent<KitchenRicePaintBridge>();
            KitchenIngredientDefinition rice = CreateDefinition(KitchenIngredientCategory.Rice, IngredientType.Rice);

            try
            {
                bridge.SetPaintingEnabled(true);
                bridge.SelectRice(rice, controller);

                Assert.IsTrue(bridge.PaintingEnabled);
                Assert.IsTrue(bridge.HasSelectedRice);

                bridge.SetPaintingEnabled(false);

                Assert.IsFalse(bridge.PaintingEnabled);
                Assert.IsFalse(bridge.HasSelectedRice);
            }
            finally
            {
                Object.DestroyImmediate(bridgeObject);
            }
        }

        [Test]
        public void RicePaintBridge_SelectRiceAppliesBrushToTransparentActiveSurface()
        {
            GameObject seaweedObject = CreateSeaweedObject(
                "SeaweedObject",
                position: Vector3.zero,
                scale: Vector3.one,
                spritePivot: new Vector2(0.5f, 0.5f));
            GameObject bridgeObject = new GameObject("KitchenRicePaintBridge");
            KitchenRicePaintBridge bridge = bridgeObject.AddComponent<KitchenRicePaintBridge>();
            KitchenIngredientDefinition seaweed = CreateDefinition(KitchenIngredientCategory.Seaweed, IngredientType.Seaweed);
            KitchenIngredientDefinition rice = CreateDefinition(KitchenIngredientCategory.Rice, IngredientType.Rice);

            try
            {
                controller.RegisterDroppedObject(seaweed, seaweedObject);
                controller.ConfigureSceneReferences(null, bridge);
                bridge.SetPaintingEnabled(true);

                controller.SelectRice(rice);

                Assert.IsTrue(bridge.HasSelectedRice);
                Assert.AreEqual(Color.clear, controller.CurrentRiceSurface.SurfaceColor);
                Assert.AreEqual(rice.RiceBrushId, controller.CurrentRiceSurface.GetSelectedBrushId());
            }
            finally
            {
                Object.DestroyImmediate(bridgeObject);
                DestroySeaweedObject(seaweedObject);
            }
        }

        private static KitchenIngredientDefinition CreateDefinition(KitchenIngredientCategory category, IngredientType ingredientType)
        {
            return CreateDefinition(category, ingredientType, Color.white);
        }

        private static KitchenIngredientDefinition CreateDefinition(KitchenIngredientCategory category, IngredientType ingredientType, Color color)
        {
            return new KitchenIngredientDefinition(
                ingredientType.ToString().ToLowerInvariant(),
                ingredientType.ToString(),
                ingredientType,
                category,
                color);
        }

        private static GameObject CreateSeaweedObject(string name, Vector3 position, Vector3 scale, Vector2 spritePivot)
        {
            GameObject seaweedObject = new GameObject(name);
            seaweedObject.transform.position = position;
            seaweedObject.transform.localScale = scale;
            Texture2D texture = new Texture2D(8, 4, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 8f, 4f), spritePivot, 1f);
            SpriteRenderer renderer = seaweedObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.green;
            return seaweedObject;
        }

        private static void DestroySeaweedObject(GameObject seaweedObject)
        {
            if (seaweedObject == null)
            {
                return;
            }

            SpriteRenderer renderer = seaweedObject.GetComponent<SpriteRenderer>();
            Sprite sprite = renderer == null ? null : renderer.sprite;
            Texture texture = sprite == null ? null : sprite.texture;
            Object.DestroyImmediate(seaweedObject);
            if (sprite != null)
            {
                Object.DestroyImmediate(sprite);
            }

            if (texture != null)
            {
                Object.DestroyImmediate(texture);
            }
        }
    }
}
