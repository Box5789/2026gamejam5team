using GameJam.Gameplay.Spreading;
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
        private GameObject cameraObject;
        private GameObject riceSurfacePrefab;
        private KitchenController controller;
        private Camera targetCamera;

        [SetUp]
        public void SetUp()
        {
            rootObject = new GameObject("KitchenRollAnimatorTestsRoot");
            controllerObject = new GameObject("KitchenController");
            controller = controllerObject.AddComponent<KitchenController>();
            controller.ConfigureLimitsForTests(2, 2, 8);

            cameraObject = new GameObject("KitchenCamera");
            targetCamera = cameraObject.AddComponent<Camera>();
            targetCamera.orthographic = true;
            targetCamera.orthographicSize = 5f;
            targetCamera.pixelRect = new Rect(0f, 0f, 1000f, 1000f);
            targetCamera.transform.position = new Vector3(0f, 0f, -10f);

            riceSurfacePrefab = CreateSprite("RiceSurfacePrefab", Vector3.zero, Vector2.one, Color.white, null);
            riceSurfacePrefab.AddComponent<SpreadableSurface>();
            riceSurfacePrefab.AddComponent<SpreadInputController>();
            controller.ConfigureRiceSurfacePrefabForTests(riceSurfacePrefab);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(rootObject);
            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(riceSurfacePrefab);
        }

        [Test]
        public void BeginAndDragRoll_FromBottomStartArea_IncreasesProgress()
        {
            GameObject seaweed = CreateSprite("Seaweed", Vector3.zero, new Vector2(3f, 3f), Color.green);
            KitchenRollAnimator animator = CreateConfiguredAnimator();
            controller.RegisterDroppedObject(CreateDefinition(KitchenIngredientCategory.Seaweed, IngredientType.Seaweed), seaweed);

            bool began = animator.BeginRollDragForTests(WorldToScreen(new Vector3(0f, -1.25f, 0f)));
            animator.DragRollForTests(WorldToScreen(new Vector3(0f, -0.25f, 0f)));

            Assert.IsTrue(began);
            Assert.AreEqual(0.5f, animator.RollProgress, 0.001f);
            Assert.Less(seaweed.transform.localScale.y, 3f);
        }

        [Test]
        public void BeginRollDrag_OutsideBottomStartArea_DoesNotChangeProgress()
        {
            GameObject seaweed = CreateSprite("Seaweed", Vector3.zero, new Vector2(3f, 3f), Color.green);
            KitchenRollAnimator animator = CreateConfiguredAnimator();
            controller.RegisterDroppedObject(CreateDefinition(KitchenIngredientCategory.Seaweed, IngredientType.Seaweed), seaweed);

            bool began = animator.BeginRollDragForTests(WorldToScreen(new Vector3(0f, 0.2f, 0f)));

            Assert.IsFalse(began);
            Assert.AreEqual(0f, animator.RollProgress, 0.001f);
        }

        [Test]
        public void TickAfterRelease_WhenBelowCompletionThreshold_UnrollsTowardZero()
        {
            GameObject seaweed = CreateSprite("Seaweed", Vector3.zero, new Vector2(3f, 3f), Color.green);
            KitchenRollAnimator animator = CreateConfiguredAnimator();
            controller.RegisterDroppedObject(CreateDefinition(KitchenIngredientCategory.Seaweed, IngredientType.Seaweed), seaweed);
            animator.BeginRollDragForTests(WorldToScreen(new Vector3(0f, -1.25f, 0f)));
            animator.DragRollForTests(WorldToScreen(new Vector3(0f, -0.25f, 0f)));

            animator.ReleaseRollForTests();
            animator.TickForTests(0.25f);

            Assert.AreEqual(0.25f, animator.RollProgress, 0.001f);
            Assert.IsFalse(animator.IsReadyToComplete);
        }

        [Test]
        public void SetRollProgress_CompressesFillingYFromOriginalSeaweedCenter()
        {
            GameObject seaweed = CreateSprite("Seaweed", Vector3.zero, new Vector2(3f, 3f), Color.green);
            GameObject bottomFilling = CreateSprite("BottomFilling", new Vector3(-0.6f, -0.8f, -0.2f), new Vector2(0.5f, 0.2f), Color.red);
            GameObject topFilling = CreateSprite("TopFilling", new Vector3(0.7f, 1.2f, -0.2f), new Vector2(0.5f, 0.2f), Color.yellow);
            Vector3 bottomOriginalPosition = bottomFilling.transform.position;
            Vector3 topOriginalPosition = topFilling.transform.position;
            Vector3 bottomOriginalScale = bottomFilling.transform.localScale;
            Vector3 topOriginalScale = topFilling.transform.localScale;
            KitchenRollAnimator animator = CreateConfiguredAnimator();

            controller.RegisterDroppedObject(CreateDefinition(KitchenIngredientCategory.Seaweed, IngredientType.Seaweed), seaweed);
            controller.RegisterDroppedObject(CreateDefinition(KitchenIngredientCategory.Filling, IngredientType.Ham), bottomFilling);
            controller.RegisterDroppedObject(CreateDefinition(KitchenIngredientCategory.Filling, IngredientType.Egg), topFilling);

            animator.SetRollProgressForTests(0f);

            Assert.AreEqual(bottomOriginalPosition, bottomFilling.transform.position);
            Assert.AreEqual(topOriginalPosition, topFilling.transform.position);

            animator.SetRollProgressForTests(0.5f);

            Assert.AreEqual(ExpectedCompressedY(bottomOriginalPosition.y, 0.5f), bottomFilling.transform.position.y, 0.001f);
            Assert.AreEqual(ExpectedCompressedY(topOriginalPosition.y, 0.5f), topFilling.transform.position.y, 0.001f);
            Assert.AreEqual(bottomOriginalPosition.x, bottomFilling.transform.position.x, 0.001f);
            Assert.AreEqual(topOriginalPosition.x, topFilling.transform.position.x, 0.001f);
            Assert.AreEqual(bottomOriginalScale, bottomFilling.transform.localScale);
            Assert.AreEqual(topOriginalScale, topFilling.transform.localScale);

            animator.SetRollProgressForTests(1f);

            Assert.AreEqual(ExpectedCompressedY(bottomOriginalPosition.y, 1f), bottomFilling.transform.position.y, 0.001f);
            Assert.AreEqual(ExpectedCompressedY(topOriginalPosition.y, 1f), topFilling.transform.position.y, 0.001f);
        }

        [Test]
        public void SetRollProgress_RefreshesFillingSnapshotWhenFillingRegistersAfterInitialSeaweedSnapshot()
        {
            GameObject seaweed = CreateSprite("Seaweed", Vector3.zero, new Vector2(3f, 3f), Color.green);
            KitchenRollAnimator animator = CreateConfiguredAnimator();

            controller.RegisterDroppedObject(CreateDefinition(KitchenIngredientCategory.Seaweed, IngredientType.Seaweed), seaweed);
            animator.SetRollProgressForTests(0f);

            GameObject filling = CreateSprite("LateFilling", new Vector3(0.3f, -0.8f, -0.2f), new Vector2(0.5f, 0.2f), Color.red);
            Vector3 originalPosition = filling.transform.position;
            controller.RegisterDroppedObject(CreateDefinition(KitchenIngredientCategory.Filling, IngredientType.Ham), filling);

            animator.SetRollProgressForTests(0.5f);

            Assert.AreEqual(ExpectedCompressedY(originalPosition.y, 0.5f), filling.transform.position.y, 0.001f);
            Assert.AreEqual(originalPosition.x, filling.transform.position.x, 0.001f);
            Assert.AreEqual(originalPosition.z, filling.transform.position.z, 0.001f);
        }

        [Test]
        public void SetRollProgress_ShowsChildSeaweedCoverFromBottomAboveFillings()
        {
            GameObject seaweed = CreateSprite("Seaweed", Vector3.zero, new Vector2(3f, 3f), Color.green);
            KitchenRollSeaweedCover cover = AddSeaweedCover(seaweed);
            GameObject filling = CreateSprite("Filling", new Vector3(0.3f, -0.8f, -0.2f), new Vector2(0.5f, 0.2f), Color.red);
            KitchenRollAnimator animator = CreateConfiguredAnimator();

            controller.RegisterDroppedObject(CreateDefinition(KitchenIngredientCategory.Seaweed, IngredientType.Seaweed), seaweed);
            controller.RegisterDroppedObject(CreateDefinition(KitchenIngredientCategory.Filling, IngredientType.Ham), filling);

            animator.SetRollProgressForTests(0f);
            Assert.IsFalse(cover.IsVisible);

            animator.SetRollProgressForTests(0.5f);

            Assert.IsTrue(cover.IsVisible);
            Assert.AreEqual(0.5f, cover.CoverRenderer.transform.localScale.y, 0.001f);
            Assert.AreEqual(
                seaweed.GetComponent<SpriteRenderer>().bounds.min.y,
                cover.CoverRenderer.bounds.min.y,
                0.001f);
            Assert.Greater(cover.CoverRenderer.sortingOrder, MaxRendererSortingOrder(filling.transform));
            Assert.AreEqual(seaweed.GetComponent<SpriteRenderer>().color, cover.CoverRenderer.color);
        }

        [Test]
        public void SetRollProgress_HidesChildSeaweedCoverWhenUnrolledToZero()
        {
            GameObject seaweed = CreateSprite("Seaweed", Vector3.zero, new Vector2(3f, 3f), Color.green);
            KitchenRollSeaweedCover cover = AddSeaweedCover(seaweed);
            KitchenRollAnimator animator = CreateConfiguredAnimator();

            controller.RegisterDroppedObject(CreateDefinition(KitchenIngredientCategory.Seaweed, IngredientType.Seaweed), seaweed);

            animator.SetRollProgressForTests(0.5f);
            Assert.IsTrue(cover.IsVisible);

            animator.SetRollProgressForTests(0f);

            Assert.IsFalse(cover.IsVisible);
        }

        [Test]
        public void TickAfterRelease_MovesFillingYBackTowardOriginalPosition()
        {
            GameObject seaweed = CreateSprite("Seaweed", Vector3.zero, new Vector2(3f, 3f), Color.green);
            GameObject filling = CreateSprite("Filling", new Vector3(0.3f, -0.8f, -0.2f), new Vector2(0.5f, 0.2f), Color.red);
            Vector3 originalPosition = filling.transform.position;
            KitchenRollAnimator animator = CreateConfiguredAnimator();

            controller.RegisterDroppedObject(CreateDefinition(KitchenIngredientCategory.Seaweed, IngredientType.Seaweed), seaweed);
            controller.RegisterDroppedObject(CreateDefinition(KitchenIngredientCategory.Filling, IngredientType.Ham), filling);

            animator.SetRollProgressForTests(0.6f);
            Assert.AreEqual(ExpectedCompressedY(originalPosition.y, 0.6f), filling.transform.position.y, 0.001f);

            animator.ReleaseRollForTests();
            animator.TickForTests(0.3f);

            Assert.AreEqual(0.3f, animator.RollProgress, 0.001f);
            Assert.AreEqual(ExpectedCompressedY(originalPosition.y, 0.3f), filling.transform.position.y, 0.001f);
            Assert.AreEqual(originalPosition.x, filling.transform.position.x, 0.001f);
        }

        [Test]
        public void FinalizeRoll_ReusesExistingObjectsAndAdjustsOnlyFillingY()
        {
            GameObject seaweed = CreateSprite("Seaweed", Vector3.zero, new Vector2(3f, 3f), Color.green);
            GameObject ham = CreateSprite("HamFilling", new Vector3(-1.8f, -0.7f, -0.2f), new Vector2(3.4f, 0.18f), Color.red);
            GameObject egg = CreateSprite("EggFilling", new Vector3(1.9f, 0.65f, -0.2f), new Vector2(3.2f, 0.18f), Color.yellow);
            Vector3 hamOriginalPosition = ham.transform.position;
            Vector3 eggOriginalPosition = egg.transform.position;
            Vector3 hamOriginalScale = ham.transform.localScale;
            Vector3 eggOriginalScale = egg.transform.localScale;
            Quaternion hamOriginalRotation = ham.transform.rotation;
            Quaternion eggOriginalRotation = egg.transform.rotation;
            KitchenRollAnimator animator = CreateConfiguredAnimator();

            controller.RegisterDroppedObject(CreateDefinition(KitchenIngredientCategory.Seaweed, IngredientType.Seaweed), seaweed);
            controller.RegisterDroppedObject(CreateDefinition(KitchenIngredientCategory.Filling, IngredientType.Ham), ham);
            controller.RegisterDroppedObject(CreateDefinition(KitchenIngredientCategory.Filling, IngredientType.Egg), egg);
            Assert.IsNotNull(controller.CurrentRiceSurface);

            animator.SetRollProgressForTests(1f);
            animator.FinalizeRoll();

            Assert.IsFalse(controller.CurrentRiceSurface.gameObject.activeSelf);
            Assert.IsNull(FindChild(rootObject.transform, "CompletedKimbapPreview"));
            Assert.IsNull(FindChild(rootObject.transform, "CompletedKimbapBody"));
            Assert.AreEqual(0, FindRenderersByPrefix(rootObject.transform, "CompletedFillingStrip_").Length);

            Assert.AreEqual(hamOriginalPosition.x, ham.transform.position.x, 0.001f);
            Assert.AreEqual(eggOriginalPosition.x, egg.transform.position.x, 0.001f);
            Assert.AreEqual(ExpectedCompressedY(hamOriginalPosition.y, 1f), ham.transform.position.y, 0.001f);
            Assert.AreEqual(ExpectedCompressedY(eggOriginalPosition.y, 1f), egg.transform.position.y, 0.001f);
            Assert.AreEqual(hamOriginalScale, ham.transform.localScale);
            Assert.AreEqual(eggOriginalScale, egg.transform.localScale);
            Assert.AreEqual(hamOriginalRotation, ham.transform.rotation);
            Assert.AreEqual(eggOriginalRotation, egg.transform.rotation);
            Assert.IsTrue(seaweed.GetComponent<SpriteRenderer>().enabled);
        }

        [Test]
        public void SetRollProgress_AtCompletionThreshold_HoldsReadyStateInsteadOfUnrolling()
        {
            GameObject seaweed = CreateSprite("Seaweed", Vector3.zero, new Vector2(3f, 3f), Color.green);
            KitchenRollAnimator animator = CreateConfiguredAnimator();
            controller.RegisterDroppedObject(CreateDefinition(KitchenIngredientCategory.Seaweed, IngredientType.Seaweed), seaweed);

            animator.SetRollProgressForTests(0.96f);
            animator.ReleaseRollForTests();
            animator.TickForTests(1f);

            Assert.AreEqual(0.96f, animator.RollProgress, 0.001f);
            Assert.IsTrue(animator.IsReadyToComplete);
        }

        [Test]
        public void FinalizeRoll_KeepsChildSeaweedCoverFullHeightAboveFillings()
        {
            GameObject seaweed = CreateSprite("Seaweed", Vector3.zero, new Vector2(3f, 3f), Color.green);
            KitchenRollSeaweedCover cover = AddSeaweedCover(seaweed);
            GameObject ham = CreateSprite("HamFilling", new Vector3(-1.8f, -0.7f, -0.2f), new Vector2(3.4f, 0.18f), Color.red);
            GameObject egg = CreateSprite("EggFilling", new Vector3(1.9f, 0.65f, -0.2f), new Vector2(3.2f, 0.18f), Color.yellow);
            KitchenRollAnimator animator = CreateConfiguredAnimator();

            controller.RegisterDroppedObject(CreateDefinition(KitchenIngredientCategory.Seaweed, IngredientType.Seaweed), seaweed);
            controller.RegisterDroppedObject(CreateDefinition(KitchenIngredientCategory.Filling, IngredientType.Ham), ham);
            controller.RegisterDroppedObject(CreateDefinition(KitchenIngredientCategory.Filling, IngredientType.Egg), egg);

            animator.SetRollProgressForTests(1f);
            animator.FinalizeRoll();

            Assert.IsTrue(cover.IsVisible);
            Assert.AreEqual(1f, cover.CoverRenderer.transform.localScale.y, 0.001f);
            Assert.Greater(cover.CoverRenderer.sortingOrder, MaxRendererSortingOrder(ham.transform));
            Assert.Greater(cover.CoverRenderer.sortingOrder, MaxRendererSortingOrder(egg.transform));
        }

        private KitchenRollAnimator CreateConfiguredAnimator()
        {
            KitchenRollAnimator animator = rootObject.AddComponent<KitchenRollAnimator>();
            animator.Configure(controller);
            animator.ConfigureTuningForTests(0.25f, 2f, 1f, 0.95f, 0.5f);
            SetPrivateField(animator, "targetCamera", targetCamera);
            return animator;
        }

        private Vector2 WorldToScreen(Vector3 worldPosition)
        {
            return targetCamera.WorldToScreenPoint(worldPosition);
        }

        private static float ExpectedCompressedY(float originalY, float progress)
        {
            const float originalSeaweedCenterY = 0f;
            const float finalSeaweedCenterY = 0.75f;
            const float finalFillingYScale = 0.5f;
            float compressedY = finalSeaweedCenterY + ((originalY - originalSeaweedCenterY) * finalFillingYScale);
            return Mathf.Lerp(originalY, compressedY, progress);
        }

        private static KitchenRollSeaweedCover AddSeaweedCover(GameObject seaweed)
        {
            GameObject coverObject = new GameObject("RollSeaweedCover");
            coverObject.transform.SetParent(seaweed.transform, false);
            coverObject.transform.localScale = new Vector3(1f, 0f, 1f);
            SpriteRenderer coverRenderer = coverObject.AddComponent<SpriteRenderer>();
            coverRenderer.sprite = KitchenPlaceholderFactory.CreateWhiteSprite();
            coverRenderer.color = Color.white;
            coverRenderer.sortingOrder = 0;
            coverObject.SetActive(false);

            KitchenRollSeaweedCover cover = seaweed.AddComponent<KitchenRollSeaweedCover>();
            SetPrivateField(cover, "coverRenderer", coverRenderer);
            return cover;
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

        private static int MaxRendererSortingOrder(Transform root)
        {
            int maxSortingOrder = int.MinValue;
            SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                maxSortingOrder = Mathf.Max(maxSortingOrder, renderers[i].sortingOrder);
            }

            return maxSortingOrder;
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            System.Reflection.FieldInfo field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"{fieldName} field is missing.");
            field.SetValue(target, value);
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
