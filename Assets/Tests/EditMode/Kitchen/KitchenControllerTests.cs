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
        private KitchenController controller;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("KitchenControllerTests");
            controller = gameObject.AddComponent<KitchenController>();
            controller.ConfigureLimitsForTests(1, 2, 1);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gameObject);
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
            KitchenIngredientDefinition seaweed = CreateDefinition(KitchenIngredientCategory.Seaweed, IngredientType.Seaweed);

            try
            {
                controller.RegisterDroppedObject(seaweed, seaweedObject);

                Assert.AreSame(seaweedObject, controller.TopSeaweedObject);
                Assert.IsNotNull(controller.CurrentRiceSurface);
                Assert.IsNotNull(controller.CurrentRiceInputController);
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
                Assert.AreEqual(secondColor, controller.CurrentRiceSurface.SurfaceColor);
            }
            finally
            {
                Object.DestroyImmediate(firstSeaweedObject);
                Object.DestroyImmediate(secondSeaweedObject);
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
    }
}
