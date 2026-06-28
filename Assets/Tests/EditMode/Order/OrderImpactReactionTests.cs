using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using KimbapGame.Data;
using KimbapGame.Order;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace KimbapGame.Tests.Order
{
    public sealed class OrderImpactReactionTests
    {
        private const string ExistingReactionPath = "Order/\uC0AC\uB78C_\uC131\uACF5_1";

        private readonly List<Object> objectsToDestroy = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = objectsToDestroy.Count - 1; i >= 0; i--)
            {
                if (objectsToDestroy[i] != null)
                {
                    Object.DestroyImmediate(objectsToDestroy[i]);
                }
            }

            objectsToDestroy.Clear();
        }

        [Test]
        public void Parse_ReadsMatchedImageColumn()
        {
            GoogleSheetOrderLoader loader = CreateLoader();
            string csv = "Index,\uB9DE\uC74C-\uC774\uBBF8\uC9C0\nc1,Order/Person_Match\n";

            List<SheetOrderData> orders = loader.Parse(csv);

            Assert.AreEqual(1, orders.Count);
            Assert.AreEqual("Order/Person_Match", orders[0].matchedImageName);
        }

        [Test]
        public void SerializedDefaults_ExposeImpactTuningValues()
        {
            OrderSceneController controller = CreateController();
            SerializedObject serialized = new SerializedObject(controller);

            Assert.AreEqual(2.5f, serialized.FindProperty("impactReactionVelocityThreshold").floatValue, 0.0001f);
            Assert.AreEqual(1f, serialized.FindProperty("impactReactionDuration").floatValue, 0.0001f);
        }

        [Test]
        public void AutoBind_AttachesImpactResponderToPersonColliderParent()
        {
            OrderSceneController controller = CreateController();
            GameObject personRoot = Track(new GameObject("PersonRoot"));
            personRoot.AddComponent<BoxCollider2D>();

            GameObject person = Track(new GameObject("Person"));
            person.transform.SetParent(personRoot.transform);
            SpriteRenderer renderer = person.AddComponent<SpriteRenderer>();

            SetPrivateField(controller, "personSpriteRenderer", renderer);
            InvokePrivate(controller, "AutoBindMissingReferences");

            OrderPersonImpactResponder responder = personRoot.GetComponent<OrderPersonImpactResponder>();
            Assert.IsNotNull(responder);
            Assert.AreSame(controller, responder.Controller);
        }

        [Test]
        public void TryPlayImpactReaction_BelowThreshold_DoesNotChangeSprite()
        {
            OrderSceneController controller = CreateConfiguredController(out SpriteRenderer renderer, out Sprite originalSprite);
            SetPrivateField(controller, "impactReactionVelocityThreshold", 2.5f);

            bool reacted = controller.TryPlayImpactReaction(2.49f);

            Assert.IsFalse(reacted);
            Assert.AreSame(originalSprite, renderer.sprite);
        }

        [UnityTest]
        public IEnumerator TryPlayImpactReaction_AtThreshold_ChangesThenRestoresSprite()
        {
            OrderSceneController controller = CreateConfiguredController(out SpriteRenderer renderer, out Sprite originalSprite);
            SetPrivateField(controller, "impactReactionVelocityThreshold", 2.5f);
            SetPrivateField(controller, "impactReactionDuration", 0f);

            Sprite expectedReactionSprite = Resources.Load<Sprite>(ExistingReactionPath);
            Assert.IsNotNull(expectedReactionSprite, $"Test sprite missing from Resources: {ExistingReactionPath}");

            bool reacted = controller.TryPlayImpactReaction(2.5f);

            Assert.IsTrue(reacted);
            Assert.AreSame(expectedReactionSprite, renderer.sprite);

            yield return null;

            Assert.AreSame(originalSprite, renderer.sprite);
        }

        [Test]
        public void TryPlayImpactReaction_WithMissingSprite_KeepsCurrentSprite()
        {
            OrderSceneController controller = CreateConfiguredController(out SpriteRenderer renderer, out Sprite originalSprite);
            SetPrivateField(controller, "currentOrder", new SheetOrderData { matchedImageName = "Order/MissingImpactSprite" });

            bool reacted = controller.TryPlayImpactReaction(99f);

            Assert.IsFalse(reacted);
            Assert.AreSame(originalSprite, renderer.sprite);
        }

        [Test]
        public void NotifyImpact_UsesControllerThreshold()
        {
            OrderSceneController controller = CreateConfiguredController(out _, out _);
            SetPrivateField(controller, "impactReactionVelocityThreshold", 2.5f);

            GameObject responderObject = Track(new GameObject("Responder"));
            responderObject.AddComponent<BoxCollider2D>();
            OrderPersonImpactResponder responder = responderObject.AddComponent<OrderPersonImpactResponder>();
            responder.Configure(controller);

            Assert.IsFalse(responder.NotifyImpact(2.49f));
            Assert.IsTrue(responder.NotifyImpact(2.5f));
        }

        private GoogleSheetOrderLoader CreateLoader()
        {
            GameObject gameObject = Track(new GameObject("Loader"));
            return gameObject.AddComponent<GoogleSheetOrderLoader>();
        }

        private OrderSceneController CreateController()
        {
            GameObject gameObject = Track(new GameObject("OrderSceneController"));
            gameObject.AddComponent<GoogleSheetOrderLoader>();
            return gameObject.AddComponent<OrderSceneController>();
        }

        private OrderSceneController CreateConfiguredController(out SpriteRenderer renderer, out Sprite originalSprite)
        {
            OrderSceneController controller = CreateController();
            GameObject person = Track(new GameObject("Person"));
            renderer = person.AddComponent<SpriteRenderer>();
            originalSprite = CreateSprite(Color.red);
            renderer.sprite = originalSprite;

            SetPrivateField(controller, "personSpriteRenderer", renderer);
            SetPrivateField(controller, "currentOrder", new SheetOrderData { matchedImageName = ExistingReactionPath });
            return controller;
        }

        private Sprite CreateSprite(Color color)
        {
            Texture2D texture = Track(new Texture2D(2, 2));
            texture.SetPixels(new[] { color, color, color, color });
            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 2f);
            return Track(sprite);
        }

        private T Track<T>(T instance) where T : Object
        {
            objectsToDestroy.Add(instance);
            return instance;
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field not found: {fieldName}");
            field.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Method not found: {methodName}");
            method.Invoke(target, null);
        }
    }
}
