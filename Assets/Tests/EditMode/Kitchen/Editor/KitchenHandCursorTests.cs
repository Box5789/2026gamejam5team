using KimbapGame.Kitchen;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace KimbapGame.Tests.Kitchen
{
    public sealed class KitchenHandCursorTests
    {
        private GameObject cameraObject;
        private GameObject cursorObject;
        private GameObject armPivotObject;
        private GameObject handPivotObject;
        private GameObject armObject;
        private GameObject handVisualObject;
        private GameObject armVisualObject;
        private Texture2D defaultTexture;
        private Texture2D pressedTexture;
        private Sprite defaultSprite;
        private Sprite pressedSprite;

        [TearDown]
        public void TearDown()
        {
            Destroy(cursorObject);
            Destroy(cameraObject);
            Destroy(defaultSprite);
            Destroy(pressedSprite);
            Destroy(defaultTexture);
            Destroy(pressedTexture);
        }

        [Test]
        public void ApplyPointerState_MovesHandPivotToScreenPoint()
        {
            KitchenHandCursor cursor = CreateCursor();

            cursor.ApplyPointerState(new Vector2(500f, 500f), false);

            AssertVector(new Vector3(0f, 0f, 0f), handPivotObject.transform.position);
        }

        [Test]
        public void ApplyPointerState_KeepsArmPivotAtViewportAnchorWithOffset()
        {
            KitchenHandCursor cursor = CreateCursor(
                armAnchorViewportPosition: new Vector2(1f, 0f),
                armPivotWorldOffset: new Vector2(1f, 2f));

            cursor.ApplyPointerState(new Vector2(500f, 500f), false);

            AssertVector(new Vector3(6f, -3f, 0f), armPivotObject.transform.position);
        }

        [Test]
        public void ApplyPointerState_RotatesArmFromArmPivotTowardHandPivot()
        {
            KitchenHandCursor cursor = CreateCursor(armAnchorViewportPosition: new Vector2(0.5f, 0.5f));

            cursor.ApplyPointerState(new Vector2(500f, 1000f), false);

            AssertAngle(90f, armObject.transform.eulerAngles.z);
        }

        [Test]
        public void ApplyPointerState_OffsetsArmFromHandPivotUsingArmRotation()
        {
            KitchenHandCursor cursor = CreateCursor(
                armAnchorViewportPosition: new Vector2(0.5f, 0.5f),
                armOffsetFromHandPivot: new Vector2(2f, 0f));

            cursor.ApplyPointerState(new Vector2(500f, 1000f), false);

            AssertVector(new Vector3(0f, 7f, 0f), armObject.transform.position);
        }

        [Test]
        public void ApplyPointerState_AppliesArmAndVisualLocalTuning()
        {
            KitchenHandCursor cursor = CreateCursor(
                armScale: new Vector3(2f, 3f, 1f),
                handVisualOffset: new Vector2(1f, 2f),
                handVisualRotationOffset: 15f,
                handVisualScale: new Vector3(1.5f, 1.25f, 1f),
                armVisualOffset: new Vector2(-1f, -2f),
                armVisualRotationOffset: -20f,
                armVisualScale: new Vector3(0.75f, 0.5f, 1f));

            cursor.ApplyPointerState(new Vector2(500f, 500f), false);

            AssertVector(new Vector3(2f, 3f, 1f), armObject.transform.localScale);
            AssertVector(new Vector3(1f, 2f, 0f), handVisualObject.transform.localPosition);
            AssertAngle(15f, handVisualObject.transform.localEulerAngles.z);
            AssertVector(new Vector3(1.5f, 1.25f, 1f), handVisualObject.transform.localScale);
            AssertVector(new Vector3(-1f, -2f, 0f), armVisualObject.transform.localPosition);
            AssertAngle(-20f, armVisualObject.transform.localEulerAngles.z);
            AssertVector(new Vector3(0.75f, 0.5f, 1f), armVisualObject.transform.localScale);
        }

        [Test]
        public void ApplyPointerState_UsesPressedSpriteWhenPressed()
        {
            KitchenHandCursor cursor = CreateCursor(defaultHandSprite: CreateDefaultSprite(), pressedHandSprite: CreatePressedSprite());

            cursor.ApplyPointerState(new Vector2(500f, 500f), false);
            Assert.AreSame(defaultSprite, cursor.CurrentHandSprite);

            cursor.ApplyPointerState(new Vector2(500f, 500f), true);
            Assert.AreSame(pressedSprite, cursor.CurrentHandSprite);
        }

        [Test]
        public void ApplyPointerState_FallsBackToDefaultSpriteWhenPressedSpriteMissing()
        {
            KitchenHandCursor cursor = CreateCursor(defaultHandSprite: CreateDefaultSprite());

            cursor.ApplyPointerState(new Vector2(500f, 500f), true);

            Assert.AreSame(defaultSprite, cursor.CurrentHandSprite);
        }

        [Test]
        public void ApplyPointerState_HidesWhenOutsideCameraAndRestoresInside()
        {
            KitchenHandCursor cursor = CreateCursor(hideWhenOutsideCamera: true);

            cursor.ApplyPointerState(new Vector2(-1f, 500f), false);

            Assert.IsFalse(cursor.IsVisible);
            Assert.IsFalse(armPivotObject.activeSelf);
            Assert.IsFalse(handPivotObject.activeSelf);
            Assert.IsFalse(armObject.activeSelf);

            cursor.ApplyPointerState(new Vector2(500f, 500f), false);

            Assert.IsTrue(cursor.IsVisible);
            Assert.IsTrue(armPivotObject.activeSelf);
            Assert.IsTrue(handPivotObject.activeSelf);
            Assert.IsTrue(armObject.activeSelf);
        }

        [Test]
        public void ApplyEditModePreview_UsesPreviewViewportPosition()
        {
            KitchenHandCursor cursor = CreateCursor(editModePreviewViewportPosition: new Vector2(1f, 1f));

            cursor.ApplyEditModePreview();

            AssertVector(new Vector3(5f, 5f, 0f), handPivotObject.transform.position);
        }

        [Test]
        public void ApplyEditModePreview_RecalculatesArmWhenInspectorValuesChange()
        {
            KitchenHandCursor cursor = CreateCursor(
                armAnchorViewportPosition: new Vector2(0.5f, 0.5f),
                editModePreviewViewportPosition: new Vector2(1f, 0.5f));
            cursor.ApplyEditModePreview();
            AssertAngle(0f, armObject.transform.eulerAngles.z);

            SerializedObject serializedObject = new SerializedObject(cursor);
            serializedObject.FindProperty("editModePreviewViewportPosition").vector2Value = new Vector2(0.5f, 1f);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            cursor.ApplyEditModePreview();

            AssertAngle(90f, armObject.transform.eulerAngles.z);
        }

        [Test]
        public void ApplyEditModePreview_AppliesVisualLocalTuning()
        {
            KitchenHandCursor cursor = CreateCursor(
                handVisualOffset: new Vector2(0.5f, -0.25f),
                handVisualRotationOffset: 30f,
                handVisualScale: new Vector3(2f, 2f, 1f),
                armVisualOffset: new Vector2(-0.5f, 0.25f),
                armVisualRotationOffset: -45f,
                armVisualScale: new Vector3(3f, 1f, 1f));

            cursor.ApplyEditModePreview();

            AssertVector(new Vector3(0.5f, -0.25f, 0f), handVisualObject.transform.localPosition);
            AssertAngle(30f, handVisualObject.transform.localEulerAngles.z);
            AssertVector(new Vector3(2f, 2f, 1f), handVisualObject.transform.localScale);
            AssertVector(new Vector3(-0.5f, 0.25f, 0f), armVisualObject.transform.localPosition);
            AssertAngle(-45f, armVisualObject.transform.localEulerAngles.z);
            AssertVector(new Vector3(3f, 1f, 1f), armVisualObject.transform.localScale);
        }

        [Test]
        public void ApplyEditModePreview_UsesPressedSpriteWhenPreviewPressed()
        {
            KitchenHandCursor cursor = CreateCursor(
                defaultHandSprite: CreateDefaultSprite(),
                pressedHandSprite: CreatePressedSprite(),
                editModePreviewPressed: true);

            cursor.ApplyEditModePreview();

            Assert.AreSame(pressedSprite, cursor.CurrentHandSprite);
        }

        [Test]
        public void ApplyEditModePreview_FallsBackToDefaultWhenPressedSpriteMissing()
        {
            KitchenHandCursor cursor = CreateCursor(
                defaultHandSprite: CreateDefaultSprite(),
                editModePreviewPressed: true);

            cursor.ApplyEditModePreview();

            Assert.AreSame(defaultSprite, cursor.CurrentHandSprite);
        }

        private KitchenHandCursor CreateCursor(
            Vector2? armAnchorViewportPosition = null,
            Vector2? armPivotWorldOffset = null,
            Vector2? armOffsetFromHandPivot = null,
            float armAngleOffset = 0f,
            Vector3? armScale = null,
            Vector2? handVisualOffset = null,
            float handVisualRotationOffset = 0f,
            Vector3? handVisualScale = null,
            Vector2? armVisualOffset = null,
            float armVisualRotationOffset = 0f,
            Vector3? armVisualScale = null,
            Sprite defaultHandSprite = null,
            Sprite pressedHandSprite = null,
            bool hideWhenOutsideCamera = false,
            bool previewInEditMode = true,
            Vector2? editModePreviewViewportPosition = null,
            bool editModePreviewPressed = false)
        {
            cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.aspect = 1f;
            camera.pixelRect = new Rect(0f, 0f, 1000f, 1000f);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            cursorObject = new GameObject("KitchenHandCursor");
            armPivotObject = new GameObject("ArmPivot");
            handPivotObject = new GameObject("HandPivot");
            armObject = new GameObject("Arm");
            handVisualObject = new GameObject("HandVisual");
            armVisualObject = new GameObject("ArmVisual");
            armPivotObject.transform.SetParent(cursorObject.transform, false);
            handPivotObject.transform.SetParent(cursorObject.transform, false);
            armObject.transform.SetParent(cursorObject.transform, false);
            handVisualObject.transform.SetParent(armObject.transform, false);
            armVisualObject.transform.SetParent(armObject.transform, false);

            SpriteRenderer handRenderer = handVisualObject.AddComponent<SpriteRenderer>();
            armVisualObject.AddComponent<SpriteRenderer>();
            KitchenHandCursor cursor = cursorObject.AddComponent<KitchenHandCursor>();

            SerializedObject serializedObject = new SerializedObject(cursor);
            serializedObject.FindProperty("targetCamera").objectReferenceValue = camera;
            serializedObject.FindProperty("armPivot").objectReferenceValue = armPivotObject.transform;
            serializedObject.FindProperty("handPivot").objectReferenceValue = handPivotObject.transform;
            serializedObject.FindProperty("armRoot").objectReferenceValue = armObject.transform;
            serializedObject.FindProperty("handVisualRoot").objectReferenceValue = handVisualObject.transform;
            serializedObject.FindProperty("armVisualRoot").objectReferenceValue = armVisualObject.transform;
            serializedObject.FindProperty("handRenderer").objectReferenceValue = handRenderer;
            serializedObject.FindProperty("defaultHandSprite").objectReferenceValue = defaultHandSprite;
            serializedObject.FindProperty("pressedHandSprite").objectReferenceValue = pressedHandSprite;
            serializedObject.FindProperty("armAnchorViewportPosition").vector2Value = armAnchorViewportPosition ?? new Vector2(1.08f, -0.1f);
            serializedObject.FindProperty("armPivotWorldOffset").vector2Value = armPivotWorldOffset ?? Vector2.zero;
            serializedObject.FindProperty("armOffsetFromHandPivot").vector2Value = armOffsetFromHandPivot ?? Vector2.zero;
            serializedObject.FindProperty("armAngleOffset").floatValue = armAngleOffset;
            serializedObject.FindProperty("armScale").vector3Value = armScale ?? Vector3.one;
            serializedObject.FindProperty("handVisualOffset").vector2Value = handVisualOffset ?? Vector2.zero;
            serializedObject.FindProperty("handVisualRotationOffset").floatValue = handVisualRotationOffset;
            serializedObject.FindProperty("handVisualScale").vector3Value = handVisualScale ?? Vector3.one;
            serializedObject.FindProperty("armVisualOffset").vector2Value = armVisualOffset ?? Vector2.zero;
            serializedObject.FindProperty("armVisualRotationOffset").floatValue = armVisualRotationOffset;
            serializedObject.FindProperty("armVisualScale").vector3Value = armVisualScale ?? Vector3.one;
            serializedObject.FindProperty("worldZ").floatValue = 0f;
            serializedObject.FindProperty("followSmoothTime").floatValue = 0f;
            serializedObject.FindProperty("hideWhenOutsideCamera").boolValue = hideWhenOutsideCamera;
            serializedObject.FindProperty("previewInEditMode").boolValue = previewInEditMode;
            serializedObject.FindProperty("editModePreviewViewportPosition").vector2Value = editModePreviewViewportPosition ?? new Vector2(0.5f, 0.5f);
            serializedObject.FindProperty("editModePreviewPressed").boolValue = editModePreviewPressed;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            return cursor;
        }

        private Sprite CreateDefaultSprite()
        {
            defaultTexture = CreateTexture(Color.white);
            defaultSprite = Sprite.Create(defaultTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            return defaultSprite;
        }

        private Sprite CreatePressedSprite()
        {
            pressedTexture = CreateTexture(Color.gray);
            pressedSprite = Sprite.Create(pressedTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            return pressedSprite;
        }

        private static Texture2D CreateTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private static void AssertVector(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 0.0001f);
            Assert.AreEqual(expected.y, actual.y, 0.0001f);
            Assert.AreEqual(expected.z, actual.z, 0.0001f);
        }

        private static void AssertAngle(float expected, float actual)
        {
            Assert.AreEqual(0f, Mathf.DeltaAngle(expected, actual), 0.0001f);
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
