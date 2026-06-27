using System.Reflection;
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
        private GameObject rollGuidePrefab;
        private GameObject completedKimbapPrefab;
        private GameObject completedFillingCapPrefab;
        private KitchenController controller;

        [SetUp]
        public void SetUp()
        {
            rootObject = new GameObject("KitchenRollAnimatorTestsRoot");
            controllerObject = new GameObject("KitchenController");
            controller = controllerObject.AddComponent<KitchenController>();
            controller.ConfigureLimitsForTests(2, 2, 4);
            rollGuidePrefab = CreateSprite("RollGuidePrefab", Vector3.zero, Vector2.one, Color.white, null);
            completedKimbapPrefab = CreateSprite("CompletedKimbapPrefab", Vector3.zero, Vector2.one, Color.white, null);
            completedFillingCapPrefab = CreateSprite("CompletedFillingCapPrefab", Vector3.zero, Vector2.one, Color.white, null);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(rootObject);
            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(rollGuidePrefab);
            Object.DestroyImmediate(completedKimbapPrefab);
            Object.DestroyImmediate(completedFillingCapPrefab);
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
            animator.ConfigurePrefabsForTests(rollGuidePrefab, completedKimbapPrefab, completedFillingCapPrefab);
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
            animator.ConfigurePrefabsForTests(rollGuidePrefab, completedKimbapPrefab, completedFillingCapPrefab);
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
        public void FinalizeRoll_CreatesFillingStripsWithoutRiceInnerPreview()
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
            animator.ConfigurePrefabsForTests(rollGuidePrefab, completedKimbapPrefab, completedFillingCapPrefab);
            animator.PrepareRollForTests();

            animator.FinalizeRoll();

            Assert.IsTrue(animator.HasCompletedKimbap);
            Assert.AreEqual(2, animator.CompletedFillingStripCount);
            Assert.AreEqual(2, animator.CompletedEndCapCount);
            Assert.IsNull(FindChild(rootObject.transform, "CompletedKimbapInner"));
            Assert.IsFalse(hamRenderer.enabled);
            Assert.IsFalse(eggRenderer.enabled);

            GameObject hamStrip = FindChild(rootObject.transform, "CompletedFillingStrip_0");
            GameObject eggStrip = FindChild(rootObject.transform, "CompletedFillingStrip_1");

            Assert.IsNotNull(hamStrip);
            Assert.IsNotNull(eggStrip);
            Assert.AreNotEqual(hamStrip.transform.localPosition.y, eggStrip.transform.localPosition.y);
        }

        [Test]
        public void FinalizeRoll_MakesFillingStripsProtrudeHorizontallyUnderBody()
        {
            GameObject seaweed = CreateSprite("Seaweed", Vector3.zero, new Vector2(3f, 3f), Color.green);
            KitchenRollAnimator animator = rootObject.AddComponent<KitchenRollAnimator>();

            controller.RegisterDroppedObject(CreateDefinition(KitchenIngredientCategory.Seaweed, IngredientType.Seaweed), seaweed);
            for (int i = 0; i < 6; i++)
            {
                GameObject filling = CreateSprite(
                    $"Filling_{i}",
                    new Vector3(0f, -0.6f + (i * 0.2f), -0.2f),
                    new Vector2(0.5f, 0.2f),
                    Color.Lerp(Color.red, Color.yellow, i / 5f));
                controller.RegisterDroppedObject(CreateDefinition(KitchenIngredientCategory.Filling, IngredientType.Ham), filling);
            }

            animator.Configure(controller);
            animator.ConfigurePrefabsForTests(rollGuidePrefab, completedKimbapPrefab, completedFillingCapPrefab);
            animator.PrepareRollForTests();

            animator.FinalizeRoll();

            Assert.AreEqual(6, animator.CompletedFillingStripCount);

            GameObject body = FindChild(rootObject.transform, "CompletedKimbapBody");
            Assert.IsNotNull(body);

            SpriteRenderer bodyRenderer = body.GetComponent<SpriteRenderer>();
            SpriteRenderer[] stripRenderers = FindRenderersByPrefix(rootObject.transform, "CompletedFillingStrip_");

            Assert.AreEqual(6, stripRenderers.Length);

            for (int i = 0; i < stripRenderers.Length; i++)
            {
                Assert.Greater(bodyRenderer.sortingOrder, stripRenderers[i].sortingOrder);
                AssertProtrudesHorizontally(bodyRenderer.bounds, stripRenderers[i].bounds);
                AssertYBoundsInside(bodyRenderer.bounds, stripRenderers[i].bounds);
            }
        }

        [Test]
        public void FinalizeRoll_UsesSerializedFillingWidthOffsetForStaticTuning()
        {
            GameObject seaweed = CreateSprite("Seaweed", Vector3.zero, new Vector2(3f, 3f), Color.green);
            GameObject filling = CreateSprite("Filling", Vector3.zero, new Vector2(0.5f, 0.2f), Color.red);
            KitchenRollAnimator animator = rootObject.AddComponent<KitchenRollAnimator>();
            const float widthOffset = 0.23f;

            controller.RegisterDroppedObject(CreateDefinition(KitchenIngredientCategory.Seaweed, IngredientType.Seaweed), seaweed);
            controller.RegisterDroppedObject(CreateDefinition(KitchenIngredientCategory.Filling, IngredientType.Ham), filling);
            animator.Configure(controller);
            animator.ConfigurePrefabsForTests(rollGuidePrefab, completedKimbapPrefab, completedFillingCapPrefab);
            SetPrivateField(animator, "completedFillingWidthOffset", widthOffset);
            animator.PrepareRollForTests();

            animator.FinalizeRoll();

            SpriteRenderer bodyRenderer = FindChild(rootObject.transform, "CompletedKimbapBody").GetComponent<SpriteRenderer>();
            SpriteRenderer stripRenderer = FindChild(rootObject.transform, "CompletedFillingStrip_0").GetComponent<SpriteRenderer>();

            Assert.AreEqual(widthOffset, bodyRenderer.bounds.min.x - stripRenderer.bounds.min.x, 0.001f);
            Assert.AreEqual(widthOffset, stripRenderer.bounds.max.x - bodyRenderer.bounds.max.x, 0.001f);
        }

        private GameObject CreateSprite(string name, Vector3 position, Vector2 size, Color color)
        {
            return CreateSprite(name, position, size, color, rootObject.transform);
        }

        private static GameObject CreateSprite(string name, Vector3 position, Vector2 size, Color color, Transform parent)
        {
            GameObject spriteObject = new GameObject(name);
            if (parent != null)
            {
                spriteObject.transform.SetParent(parent, false);
            }

            spriteObject.transform.localPosition = position;
            spriteObject.transform.localScale = new Vector3(size.x, size.y, 1f);
            SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
            renderer.sprite = KitchenPlaceholderFactory.CreateWhiteSprite();
            renderer.color = color;
            renderer.sortingOrder = 10;
            return spriteObject;
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

        private static SpriteRenderer[] FindRenderersByPrefix(Transform root, string objectNamePrefix)
        {
            SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            System.Collections.Generic.List<SpriteRenderer> matches = new System.Collections.Generic.List<SpriteRenderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].name.StartsWith(objectNamePrefix, System.StringComparison.Ordinal))
                {
                    matches.Add(renderers[i]);
                }
            }

            return matches.ToArray();
        }

        private static void AssertProtrudesHorizontally(Bounds body, Bounds strip)
        {
            const float tolerance = 0.001f;
            Assert.Less(strip.min.x, body.min.x - tolerance);
            Assert.Greater(strip.max.x, body.max.x + tolerance);
        }

        private static void AssertYBoundsInside(Bounds outer, Bounds inner)
        {
            const float tolerance = 0.001f;
            Assert.GreaterOrEqual(inner.min.y + tolerance, outer.min.y);
            Assert.LessOrEqual(inner.max.y - tolerance, outer.max.y);
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"{fieldName} field is missing.");
            field.SetValue(target, value);
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
