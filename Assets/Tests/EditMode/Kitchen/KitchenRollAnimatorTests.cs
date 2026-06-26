using KimbapGame.Data;
using KimbapGame.Kitchen;
using NUnit.Framework;
using UnityEngine;

namespace KimbapGame.Tests.Kitchen
{
    public sealed class KitchenRollAnimatorTests
    {
        private GameObject rootObject;
        private GameObject controllerObject;
        private KitchenController controller;

        [SetUp]
        public void SetUp()
        {
            rootObject = new GameObject("KitchenRollAnimatorTestsRoot");
            controllerObject = new GameObject("KitchenController");
            controller = controllerObject.AddComponent<KitchenController>();
            controller.ConfigureLimitsForTests(2, 2, 4);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(rootObject);
            Object.DestroyImmediate(controllerObject);
        }

        [Test]
        public void SimulateRollStep_CapturesOverlappingFillingAndMovesItWithRoll()
        {
            GameObject seaweed = CreateSprite("Seaweed", Vector3.zero, new Vector2(3f, 3f), Color.green);
            GameObject bottomFilling = CreateSprite("BottomFilling", new Vector3(0f, -1.1f, -0.2f), new Vector2(0.5f, 0.2f), Color.red);
            GameObject topFilling = CreateSprite("TopFilling", new Vector3(0f, 1.4f, -0.2f), new Vector2(0.5f, 0.2f), Color.yellow);
            KitchenRollAnimator animator = rootObject.AddComponent<KitchenRollAnimator>();

            controller.RegisterDroppedObject(CreateDefinition(KitchenIngredientCategory.Seaweed, IngredientType.Seaweed), seaweed);
            controller.RegisterDroppedObject(CreateDefinition(KitchenIngredientCategory.Filling, IngredientType.Ham), bottomFilling);
            controller.RegisterDroppedObject(CreateDefinition(KitchenIngredientCategory.Filling, IngredientType.Egg), topFilling);
            animator.Configure(controller);
            animator.PrepareRollForTests();

            animator.SimulateRollStepForTests(0f);
            float capturedStartY = bottomFilling.transform.position.y;

            Assert.AreEqual(1, animator.CapturedFillingCount);

            animator.SimulateRollStepForTests(0.5f);

            Assert.AreEqual(1, animator.CapturedFillingCount);
            Assert.Greater(bottomFilling.transform.position.y, capturedStartY);
            Assert.AreEqual(1.4f, topFilling.transform.position.y, 0.001f);
            AssertNearRollCenter(animator.RollBoundsForTests, bottomFilling.transform.position);
            Assert.Less(bottomFilling.GetComponent<SpriteRenderer>().sortingOrder, animator.RollSortingOrderForTests);
        }

        [Test]
        public void SimulateRollStep_PlacesCapturedFillingsInDistinctRollSlots()
        {
            GameObject seaweed = CreateSprite("Seaweed", Vector3.zero, new Vector2(3f, 3f), Color.green);
            GameObject ham = CreateSprite("HamFilling", new Vector3(-0.4f, -1.1f, -0.2f), new Vector2(3.4f, 0.18f), Color.red);
            GameObject egg = CreateSprite("EggFilling", new Vector3(0.4f, -1.1f, -0.2f), new Vector2(3.4f, 0.18f), Color.yellow);
            Vector3 hamScale = ham.transform.localScale;
            Vector3 eggScale = egg.transform.localScale;
            KitchenRollAnimator animator = rootObject.AddComponent<KitchenRollAnimator>();

            controller.RegisterDroppedObject(CreateDefinition(KitchenIngredientCategory.Seaweed, IngredientType.Seaweed), seaweed);
            controller.RegisterDroppedObject(CreateDefinition(KitchenIngredientCategory.Filling, IngredientType.Ham), ham);
            controller.RegisterDroppedObject(CreateDefinition(KitchenIngredientCategory.Filling, IngredientType.Egg), egg);
            animator.Configure(controller);
            animator.PrepareRollForTests();

            animator.SimulateRollStepForTests(0f);
            animator.SimulateRollStepForTests(0.35f);

            Assert.AreEqual(2, animator.CapturedFillingCount);
            Assert.AreEqual(hamScale, ham.transform.localScale);
            Assert.AreEqual(eggScale, egg.transform.localScale);
            AssertNearRollCenter(animator.RollBoundsForTests, ham.transform.position);
            AssertNearRollCenter(animator.RollBoundsForTests, egg.transform.position);
            Assert.AreNotEqual(ham.transform.position.y, egg.transform.position.y);
            Assert.Less(ham.GetComponent<SpriteRenderer>().sortingOrder, animator.RollSortingOrderForTests);
            Assert.Less(egg.GetComponent<SpriteRenderer>().sortingOrder, animator.RollSortingOrderForTests);
        }

        [Test]
        public void FinalizeRoll_CreatesEndCapsWithoutRiceInnerPreview()
        {
            GameObject seaweed = CreateSprite("Seaweed", Vector3.zero, new Vector2(3f, 3f), Color.green);
            GameObject ham = CreateSprite("HamFilling", new Vector3(0f, -0.2f, -0.2f), new Vector2(0.5f, 0.2f), Color.red);
            GameObject egg = CreateSprite("EggFilling", new Vector3(0f, 0.2f, -0.2f), new Vector2(0.5f, 0.2f), Color.yellow);
            SpriteRenderer hamRenderer = ham.GetComponent<SpriteRenderer>();
            SpriteRenderer eggRenderer = egg.GetComponent<SpriteRenderer>();
            KitchenRollAnimator animator = rootObject.AddComponent<KitchenRollAnimator>();

            controller.RegisterDroppedObject(CreateDefinition(KitchenIngredientCategory.Seaweed, IngredientType.Seaweed), seaweed);
            controller.RegisterDroppedObject(CreateDefinition(KitchenIngredientCategory.Filling, IngredientType.Ham), ham);
            controller.RegisterDroppedObject(CreateDefinition(KitchenIngredientCategory.Filling, IngredientType.Egg), egg);
            animator.Configure(controller);
            animator.PrepareRollForTests();

            animator.FinalizeRoll();

            Assert.IsTrue(animator.HasCompletedKimbap);
            Assert.AreEqual(4, animator.CompletedEndCapCount);
            Assert.IsNull(FindChild(rootObject.transform, "CompletedKimbapInner"));
            Assert.IsFalse(hamRenderer.enabled);
            Assert.IsFalse(eggRenderer.enabled);

            GameObject leftHam = FindChild(rootObject.transform, "CompletedFillingLeft_0");
            GameObject leftEgg = FindChild(rootObject.transform, "CompletedFillingLeft_1");

            Assert.IsNotNull(leftHam);
            Assert.IsNotNull(leftEgg);
            Assert.AreNotEqual(leftHam.transform.localPosition.y, leftEgg.transform.localPosition.y);
        }

        private GameObject CreateSprite(string name, Vector3 position, Vector2 size, Color color)
        {
            return KitchenPlaceholderFactory.CreateSpriteObject(
                name,
                rootObject.transform,
                position,
                size,
                color,
                10);
        }

        private static GameObject FindChild(Transform root, string objectName)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name == objectName)
                {
                    return children[i].gameObject;
                }
            }

            return null;
        }

        private static void AssertNearRollCenter(Bounds rollBounds, Vector3 position)
        {
            const float tolerance = 0.001f;
            Assert.AreEqual(rollBounds.center.x, position.x, tolerance);
            Assert.LessOrEqual(Mathf.Abs(position.y - rollBounds.center.y), rollBounds.size.y * 0.151f);
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
