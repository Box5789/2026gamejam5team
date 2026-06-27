using UnityEngine;
using UnityEngine.Serialization;

namespace KimbapGame.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    [AddComponentMenu("Gameplay/Mouse Spring Return 2D")]
    public sealed class MouseSpringReturn2D : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Vector2 anchorLocalPoint = new Vector2(0f, -0.5f);
        [FormerlySerializedAs("returnSpring")]
        [SerializeField] private float angleSpring = 80f;
        [FormerlySerializedAs("returnDamping")]
        [SerializeField] private float angleDamping = 12f;
        [SerializeField] private float dragFollowSpeed = 20f;
        [SerializeField] private float maxDragAngle = 75f;

        private Rigidbody2D body;
        private Vector2 fixedAnchorWorld;
        private float restAngle;
        private float currentAngle;
        private float targetAngle;
        private float angleVelocity;
        private float dragAngleOffset;
        private float originalGravityScale;
        private bool dragging;
        private bool initialized;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void OnEnable()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            InitializePose();
            originalGravityScale = body.gravityScale;
            body.gravityScale = 0f;
        }

        private void OnDisable()
        {
            if (body != null)
            {
                body.gravityScale = originalGravityScale;
            }

            dragging = false;
        }

        private void OnMouseDown()
        {
            if (!EnsureCamera())
            {
                return;
            }

            if (!initialized)
            {
                InitializePose();
            }

            dragging = true;

            if (TryGetMouseAngle(out float mouseAngle))
            {
                dragAngleOffset = Mathf.DeltaAngle(mouseAngle, currentAngle);
                targetAngle = ClampTargetAngle(mouseAngle + dragAngleOffset);
            }
        }

        private void Update()
        {
            if (!dragging)
            {
                return;
            }

            if (Input.GetMouseButtonUp(0))
            {
                dragging = false;
                targetAngle = restAngle;
                return;
            }

            if (TryGetMouseAngle(out float mouseAngle))
            {
                float desiredAngle = ClampTargetAngle(mouseAngle + dragAngleOffset);
                float follow = 1f - Mathf.Exp(-Mathf.Max(0f, dragFollowSpeed) * Time.deltaTime);
                targetAngle = Mathf.LerpAngle(targetAngle, desiredAngle, follow);
            }
        }

        private void FixedUpdate()
        {
            if (!initialized)
            {
                InitializePose();
            }

            if (!dragging)
            {
                targetAngle = restAngle;
            }

            StepAngle();
            ApplyPose();
        }

        private void InitializePose()
        {
            fixedAnchorWorld = transform.TransformPoint(anchorLocalPoint);
            restAngle = body.rotation;
            currentAngle = restAngle;
            targetAngle = restAngle;
            angleVelocity = 0f;
            initialized = true;
        }

        private void StepAngle()
        {
            float delta = Mathf.DeltaAngle(currentAngle, targetAngle);
            angleVelocity += delta * Mathf.Max(0f, angleSpring) * Time.fixedDeltaTime;
            angleVelocity *= Mathf.Exp(-Mathf.Max(0f, angleDamping) * Time.fixedDeltaTime);
            currentAngle += angleVelocity * Time.fixedDeltaTime;
        }

        private void ApplyPose()
        {
            Vector2 anchorOffset = GetAnchorOffsetWorld(currentAngle);
            Vector2 bodyPosition = fixedAnchorWorld - anchorOffset;

            body.MoveRotation(currentAngle);
            body.MovePosition(bodyPosition);
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        private Vector2 GetAnchorOffsetWorld(float angle)
        {
            Vector2 scaledAnchor = Vector2.Scale(anchorLocalPoint, transform.lossyScale);
            return Quaternion.Euler(0f, 0f, angle) * scaledAnchor;
        }

        private float ClampTargetAngle(float angle)
        {
            if (maxDragAngle <= 0f)
            {
                return angle;
            }

            float delta = Mathf.Clamp(Mathf.DeltaAngle(restAngle, angle), -maxDragAngle, maxDragAngle);
            return restAngle + delta;
        }

        private bool TryGetMouseAngle(out float angle)
        {
            Vector2 toMouse = GetMouseWorldPosition() - fixedAnchorWorld;
            if (toMouse.sqrMagnitude < 0.0001f)
            {
                angle = currentAngle;
                return false;
            }

            angle = Mathf.Atan2(toMouse.y, toMouse.x) * Mathf.Rad2Deg;
            return true;
        }

        private Vector2 GetMouseWorldPosition()
        {
            Vector3 screenPosition = Input.mousePosition;
            screenPosition.z = Mathf.Abs(targetCamera.transform.position.z - transform.position.z);
            return targetCamera.ScreenToWorldPoint(screenPosition);
        }

        private bool EnsureCamera()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            return targetCamera != null;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 currentAnchor = transform.TransformPoint(anchorLocalPoint);
            Vector3 anchor = Application.isPlaying && initialized ? (Vector3)fixedAnchorWorld : currentAnchor;

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(anchor, 0.08f);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(anchor, currentAnchor);
        }
    }
}
