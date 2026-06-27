using UnityEngine;

namespace KimbapGame.Kitchen
{
    [DisallowMultipleComponent]
    public sealed class KitchenHandCursor : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform armPivot;
        [SerializeField] private Transform handPivot;
        [SerializeField] private Transform armRoot;
        [SerializeField] private Transform handVisualRoot;
        [SerializeField] private Transform armVisualRoot;
        [SerializeField] private SpriteRenderer handRenderer;
        [SerializeField] private Sprite defaultHandSprite;
        [SerializeField] private Sprite pressedHandSprite;
        [SerializeField] private Vector2 armAnchorViewportPosition = new Vector2(1.08f, -0.1f);
        [SerializeField] private Vector2 armPivotWorldOffset;
        [SerializeField] private Vector2 armOffsetFromHandPivot;
        [SerializeField] private float armAngleOffset;
        [SerializeField] private Vector3 armScale = Vector3.one;
        [SerializeField] private Vector2 handVisualOffset = new Vector2(0.25f, -0.15f);
        [SerializeField] private float handVisualRotationOffset;
        [SerializeField] private Vector3 handVisualScale = Vector3.one;
        [SerializeField] private Vector2 armVisualOffset;
        [SerializeField] private float armVisualRotationOffset;
        [SerializeField] private Vector3 armVisualScale = Vector3.one;
        [SerializeField] private float worldZ = -3f;
        [SerializeField] private float followSmoothTime = 0.04f;
        [SerializeField] private bool hideWhenOutsideCamera = true;
        [SerializeField] private bool previewInEditMode = true;
        [SerializeField] private Vector2 editModePreviewViewportPosition = new Vector2(0.5f, 0.5f);
        [SerializeField] private bool editModePreviewPressed;

        private Vector3 handVelocity;
        private bool hasHandPosition;
        private Sprite runtimeDefaultHandSprite;

        public Sprite CurrentHandSprite => handRenderer == null ? null : handRenderer.sprite;
        public bool IsVisible =>
            IsTransformVisible(armPivot) &&
            IsTransformVisible(handPivot) &&
            IsTransformVisible(armRoot);

        private void Awake()
        {
            CacheDefaultHandSprite();
            ApplyHandSprite(false);
        }

        private void LateUpdate()
        {
            if (TryGetPointerState(out Vector2 screenPosition, out bool pressed))
            {
                ApplyPointerState(screenPosition, pressed);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            CacheDefaultHandSprite();
            ApplyEditModePreview();
        }
#endif

        public void ApplyPointerState(Vector2 screenPosition, bool pressed)
        {
            Camera camera = ResolveCamera();
            ApplyPointerState(camera, screenPosition, pressed, false);
        }

        public void ApplyEditModePreview()
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (!previewInEditMode)
            {
                SetCursorVisible(true);
                ApplyHandSprite(false);
                return;
            }

            if (targetCamera == null)
            {
                ApplyHandSprite(editModePreviewPressed);
                return;
            }

            Vector3 screenPosition = targetCamera.ViewportToScreenPoint(new Vector3(
                editModePreviewViewportPosition.x,
                editModePreviewViewportPosition.y,
                0f));
            ApplyPointerState(targetCamera, screenPosition, editModePreviewPressed, true);
        }

        private void ApplyPointerState(Camera camera, Vector2 screenPosition, bool pressed, bool immediate)
        {
            if (camera == null || armPivot == null || handPivot == null || armRoot == null)
            {
                return;
            }

            bool isInsideCamera = camera.pixelRect.Contains(screenPosition);
            SetCursorVisible(!hideWhenOutsideCamera || isInsideCamera);
            if (hideWhenOutsideCamera && !isInsideCamera)
            {
                return;
            }

            Vector3 targetPosition = ScreenToCursorWorldPoint(camera, screenPosition);

            if (immediate || followSmoothTime <= 0f || !hasHandPosition)
            {
                handPivot.position = targetPosition;
                handVelocity = Vector3.zero;
                hasHandPosition = true;
            }
            else
            {
                handPivot.position = Vector3.SmoothDamp(
                    handPivot.position,
                    targetPosition,
                    ref handVelocity,
                    followSmoothTime);
            }

            ApplyHandSprite(pressed);
            ApplyRigPose(camera);
        }

        public Vector3 ScreenToCursorWorldPoint(Vector2 screenPosition)
        {
            Camera camera = ResolveCamera();
            if (camera == null)
            {
                return Vector3.zero;
            }

            return ScreenToCursorWorldPoint(camera, screenPosition);
        }

        public Vector3 ViewportToCursorWorldPoint(Vector2 viewportPosition)
        {
            Camera camera = ResolveCamera();
            if (camera == null)
            {
                return Vector3.zero;
            }

            return ViewportToCursorWorldPoint(camera, viewportPosition);
        }

        public bool IsScreenPointInsideCamera(Vector2 screenPosition)
        {
            Camera camera = ResolveCamera();
            return camera != null && camera.pixelRect.Contains(screenPosition);
        }

        private Vector3 ScreenToCursorWorldPoint(Camera camera, Vector2 screenPosition)
        {
            float distanceToPlane = Mathf.Abs(camera.transform.position.z - worldZ);
            Vector3 worldPoint = camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, distanceToPlane));
            worldPoint.z = worldZ;
            return worldPoint;
        }

        private Vector3 ViewportToCursorWorldPoint(Camera camera, Vector2 viewportPosition)
        {
            float distanceToPlane = Mathf.Abs(camera.transform.position.z - worldZ);
            Vector3 worldPoint = camera.ViewportToWorldPoint(new Vector3(viewportPosition.x, viewportPosition.y, distanceToPlane));
            worldPoint.z = worldZ;
            return worldPoint;
        }

        private void ApplyRigPose(Camera camera)
        {
            Vector3 handPosition = handPivot.position;
            Vector3 pivotPosition = ViewportToCursorWorldPoint(camera, armAnchorViewportPosition);
            pivotPosition.x += armPivotWorldOffset.x;
            pivotPosition.y += armPivotWorldOffset.y;
            armPivot.position = pivotPosition;

            Vector3 direction = handPosition - pivotPosition;
            float angle = armRoot.eulerAngles.z;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                angle = armRoot.eulerAngles.z;
            }
            else
            {
                angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + armAngleOffset;
            }

            Quaternion armRotation = Quaternion.Euler(0f, 0f, angle);
            armRoot.position = handPosition + armRotation * new Vector3(armOffsetFromHandPivot.x, armOffsetFromHandPivot.y, 0f);
            armRoot.rotation = armRotation;
            armRoot.localScale = armScale;

            ApplyVisualPose(handVisualRoot, handVisualOffset, handVisualRotationOffset, handVisualScale);
            ApplyVisualPose(armVisualRoot, armVisualOffset, armVisualRotationOffset, armVisualScale);
        }

        private bool TryGetPointerState(out Vector2 screenPosition, out bool pressed)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Canceled || touch.phase == TouchPhase.Ended)
                {
                    continue;
                }

                screenPosition = touch.position;
                pressed = true;
                return true;
            }

            screenPosition = Input.mousePosition;
            pressed = Input.GetMouseButton(0);
            return true;
        }

        private Camera ResolveCamera()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            return targetCamera;
        }

        private void ApplyHandSprite(bool pressed)
        {
            if (handRenderer == null)
            {
                return;
            }

            CacheDefaultHandSprite();

            Sprite fallbackSprite = defaultHandSprite != null ? defaultHandSprite : runtimeDefaultHandSprite;
            Sprite desiredSprite = pressed && pressedHandSprite != null ? pressedHandSprite : fallbackSprite;
            if (desiredSprite != null)
            {
                handRenderer.sprite = desiredSprite;
            }
        }

        private void CacheDefaultHandSprite()
        {
            if (runtimeDefaultHandSprite != null || handRenderer == null)
            {
                return;
            }

            if (defaultHandSprite != null)
            {
                runtimeDefaultHandSprite = defaultHandSprite;
                return;
            }

            if (handRenderer.sprite != null && handRenderer.sprite != pressedHandSprite)
            {
                runtimeDefaultHandSprite = handRenderer.sprite;
            }
        }

        private void SetCursorVisible(bool visible)
        {
            SetTransformVisible(armPivot, visible);
            SetTransformVisible(handPivot, visible);
            SetTransformVisible(armRoot, visible);
        }

        private static void ApplyVisualPose(Transform visualRoot, Vector2 offset, float rotationOffset, Vector3 scale)
        {
            if (visualRoot == null)
            {
                return;
            }

            visualRoot.localPosition = new Vector3(offset.x, offset.y, 0f);
            visualRoot.localRotation = Quaternion.Euler(0f, 0f, rotationOffset);
            visualRoot.localScale = scale;
        }

        private static bool IsTransformVisible(Transform target)
        {
            return target == null || target.gameObject.activeSelf;
        }

        private static void SetTransformVisible(Transform target, bool visible)
        {
            if (target != null && target.gameObject.activeSelf != visible)
            {
                target.gameObject.SetActive(visible);
            }
        }
    }
}
