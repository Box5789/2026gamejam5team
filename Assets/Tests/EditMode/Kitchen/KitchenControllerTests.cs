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
            KitchenIngredientDefinition rice = CreateDefinition(KitchenIngredientCategory.Rice, IngredientType.Rice);

            Assert.IsTrue(controller.TryAddRice(rice));
            Assert.IsTrue(controller.TryAddRice(rice));
            Assert.IsFalse(controller.TryAddRice(rice));
            Assert.AreEqual(2, controller.PreparedKimbap.riceItems.Count);
        }

        [Test]
        public void TryAddFilling_RespectsConfiguredLimit()
        {
            KitchenIngredientDefinition filling = CreateDefinition(KitchenIngredientCategory.Filling, IngredientType.Ham);

            Assert.IsTrue(controller.TryAddFilling(filling));
            Assert.IsFalse(controller.TryAddFilling(filling));
            Assert.AreEqual(1, controller.PreparedKimbap.fillings.Count);
        }

        private static KitchenIngredientDefinition CreateDefinition(KitchenIngredientCategory category, IngredientType ingredientType)
        {
            return new KitchenIngredientDefinition(
                ingredientType.ToString().ToLowerInvariant(),
                ingredientType.ToString(),
                ingredientType,
                category,
                Color.white);
        }
    }
}
