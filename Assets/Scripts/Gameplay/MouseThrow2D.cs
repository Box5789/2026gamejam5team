using UnityEngine;

namespace KimbapGame.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    [AddComponentMenu("Gameplay/Mouse Throw 2D")]
    public sealed class MouseThrow2D : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float throwPower = 1f;
        [SerializeField] private float maxThrowSpeed = 25f;

        private Rigidbody2D body;
        private Vector2 grabOffset;
        private Vector2 targetPosition;
        private Vector2 dragVelocity;
        private float lastSampleTime;
        private float originalGravityScale;
        private bool dragging;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void OnMouseDown()
        {
            if (targetCamera == null)
            {
                return;
            }

            dragging = true;
            originalGravityScale = body.gravityScale;
            body.gravityScale = 0f;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;

            Vector2 mouseWorld = GetMouseWorldPosition();
            grabOffset = body.position - mouseWorld;
            targetPosition = mouseWorld + grabOffset;
            dragVelocity = Vector2.zero;
            lastSampleTime = Time.time;
        }

        private void Update()
        {
            if (!dragging)
            {
                return;
            }

            Vector2 nextPosition = GetMouseWorldPosition() + grabOffset;
            float dt = Mathf.Max(Time.time - lastSampleTime, Time.deltaTime);
            dt = Mathf.Max(dt, 0.0001f);

            dragVelocity = (nextPosition - targetPosition) / dt;
            targetPosition = nextPosition;
            lastSampleTime = Time.time;

            if (Input.GetMouseButtonUp(0))
            {
                Release();
            }
        }

        private void FixedUpdate()
        {
            if (dragging)
            {
                body.MovePosition(targetPosition);
            }
        }

        private void OnDisable()
        {
            if (dragging && body != null)
            {
                body.gravityScale = originalGravityScale;
                dragging = false;
            }
        }

        private void Release()
        {
            dragging = false;
            body.gravityScale = originalGravityScale;
            body.linearVelocity = Vector2.ClampMagnitude(dragVelocity * throwPower, maxThrowSpeed);
        }

        private Vector2 GetMouseWorldPosition()
        {
            Vector3 screenPosition = Input.mousePosition;
            screenPosition.z = Mathf.Abs(targetCamera.transform.position.z - transform.position.z);
            return targetCamera.ScreenToWorldPoint(screenPosition);
        }
    }
}
