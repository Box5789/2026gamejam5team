using KimbapGame.Gameplay;
using KimbapGame.Data;
using KimbapGame.Kitchen;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace KimbapGame.Tests.Kitchen
{
    public sealed class KitchenDropZoneTests
    {
        [Test]
        public void DraggableSeaweed_DropsAtDropZoneCenter()
        {
            GameObject controllerObject = new GameObject("KitchenController");
            GameObject dropZoneObject = new GameObject("KitchenDropZone");
            GameObject previewObject = CreatePreview("SeaweedPreview", new Vector2(2.4f, 1.7f));
            KitchenController controller = controllerObject.AddComponent<KitchenController>();
            KitchenDropZone dropZone = dropZoneObject.AddComponent<KitchenDropZone>();
            KitchenIngredientDefinition seaweed = CreateDefinition(KitchenIngredientCategory.Seaweed, IngredientType.Seaweed);

            try
            {
                dropZoneObject.transform.position = new Vector3(2f, 3f, 0f);
                previewObject.transform.position = new Vector3(3f, 3.5f, -1f);
                KitchenDraggableItem draggableItem = previewObject.AddComponent<KitchenDraggableItem>();
                draggableItem.Initialize(controller, dropZone, seaweed, null);

                draggableItem.SendMessage("Drop", SendMessageOptions.RequireReceiver);

                Assert.AreEqual(dropZone.GetCenterWorldPoint(), previewObject.transform.position);
            }
            finally
            {
                Object.DestroyImmediate(previewObject);
                Object.DestroyImmediate(dropZoneObject);
                Object.DestroyImmediate(controllerObject);
            }
        }

        [Test]
        public void GetSnappedWorldPoint_KeepsFillingAtClampedDropPosition()
        {
            GameObject dropZoneObject = new GameObject("KitchenDropZone");
            KitchenDropZone dropZone = dropZoneObject.AddComponent<KitchenDropZone>();

            try
            {
                Vector3 requestedPoint = new Vector3(1f, 0.5f, -1f);
                Vector3 snappedPoint = dropZone.GetSnappedWorldPoint(requestedPoint);

                Assert.AreEqual(requestedPoint.x, snappedPoint.x);
                Assert.AreEqual(requestedPoint.y, snappedPoint.y);
                Assert.AreNotEqual(dropZone.GetCenterWorldPoint(), snappedPoint);
            }
            finally
            {
                Object.DestroyImmediate(dropZoneObject);
            }
        }

        [Test]
        public void PrepareThrowableRoll_EnablesColliderAndKeepsBodyKinematicWithThrowDisabled()
        {
            GameObject dropZoneObject = CreateThrowableDropZone(out KitchenDropZone dropZone, out BoxCollider2D pickupCollider, out Rigidbody2D body, out MouseThrow2D mouseThrow);

            try
            {
                pickupCollider.enabled = false;
                body.bodyType = RigidbodyType2D.Dynamic;
                mouseThrow.enabled = true;

                dropZone.PrepareThrowableRoll();

                Assert.IsTrue(dropZone.IsThrowableRollPrepared);
                Assert.IsTrue(pickupCollider.enabled);
                Assert.AreEqual(RigidbodyType2D.Kinematic, body.bodyType);
                Assert.IsFalse(mouseThrow.enabled);
            }
            finally
            {
                Object.DestroyImmediate(dropZoneObject);
            }
        }

        [Test]
        public void ActivateThrowableRoll_BeforePrepareDoesNotEnableThrow()
        {
            GameObject dropZoneObject = CreateThrowableDropZone(out KitchenDropZone dropZone, out BoxCollider2D pickupCollider, out Rigidbody2D body, out MouseThrow2D mouseThrow);

            try
            {
                pickupCollider.enabled = false;
                body.bodyType = RigidbodyType2D.Kinematic;
                mouseThrow.enabled = false;

                bool activated = dropZone.ActivateThrowableRollFromCurrentMouse();

                Assert.IsFalse(activated);
                Assert.IsFalse(pickupCollider.enabled);
                Assert.AreEqual(RigidbodyType2D.Kinematic, body.bodyType);
                Assert.IsFalse(mouseThrow.enabled);
                Assert.IsFalse(mouseThrow.IsDragging);
            }
            finally
            {
                Object.DestroyImmediate(dropZoneObject);
            }
        }

        [Test]
        public void ActivateThrowableRoll_AfterPrepareMakesDynamicAndStartsMouseThrow()
        {
            GameObject cameraObject = new GameObject("KitchenCamera");
            GameObject dropZoneObject = CreateThrowableDropZone(out KitchenDropZone dropZone, out _, out Rigidbody2D body, out MouseThrow2D mouseThrow);
            Camera camera = cameraObject.AddComponent<Camera>();
            SetMouseThrowCamera(mouseThrow, camera);

            try
            {
                dropZone.PrepareThrowableRoll();

                bool activated = dropZone.ActivateThrowableRollFromCurrentMouse();

                Assert.IsTrue(activated);
                Assert.AreEqual(RigidbodyType2D.Dynamic, body.bodyType);
                Assert.IsTrue(mouseThrow.enabled);
                Assert.IsTrue(mouseThrow.IsDragging);
            }
            finally
            {
                Object.DestroyImmediate(dropZoneObject);
                Object.DestroyImmediate(cameraObject);
            }
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

        private static GameObject CreatePreview(string name, Vector2 size)
        {
            GameObject previewObject = new GameObject(name);
            previewObject.transform.localScale = new Vector3(size.x, size.y, 1f);
            SpriteRenderer renderer = previewObject.AddComponent<SpriteRenderer>();
            renderer.sprite = KitchenPlaceholderFactory.CreateWhiteSprite();
            previewObject.AddComponent<BoxCollider2D>();
            return previewObject;
        }

        private static GameObject CreateThrowableDropZone(
            out KitchenDropZone dropZone,
            out BoxCollider2D pickupCollider,
            out Rigidbody2D body,
            out MouseThrow2D mouseThrow)
        {
            GameObject dropZoneObject = new GameObject("KitchenDropZone");
            pickupCollider = dropZoneObject.AddComponent<BoxCollider2D>();
            body = dropZoneObject.AddComponent<Rigidbody2D>();
            mouseThrow = dropZoneObject.AddComponent<MouseThrow2D>();
            mouseThrow.enabled = false;
            dropZone = dropZoneObject.AddComponent<KitchenDropZone>();
            return dropZoneObject;
        }

        private static void SetMouseThrowCamera(MouseThrow2D mouseThrow, Camera camera)
        {
            SerializedObject serializedObject = new SerializedObject(mouseThrow);
            serializedObject.FindProperty("targetCamera").objectReferenceValue = camera;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
