using UnityEngine;

namespace KimbapGame.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    [AddComponentMenu("Gameplay/Mouse Throw 2D")]
    public sealed class MouseThrow2D : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float dragForce = 120f;
        [SerializeField] private float dragDamping = 12f;
        [SerializeField] private float maxForce = 1500f;

        private Rigidbody2D body;
        private Vector2 grabLocalPoint;
        private Vector2 mouseWorldPosition;
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
            if (!EnsureCamera())
            {
                return;
            }

            dragging = true;
            mouseWorldPosition = GetMouseWorldPosition();
            grabLocalPoint = transform.InverseTransformPoint(mouseWorldPosition);
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
                return;
            }

            mouseWorldPosition = GetMouseWorldPosition();
        }

        private void FixedUpdate()
        {
            if (dragging)
            {
                ApplyDragForce();
            }
        }

        private void OnDisable()
        {
            dragging = false;
        }

        private void ApplyDragForce()
        {
            Vector2 grabWorld = transform.TransformPoint(grabLocalPoint);
            Vector2 pointVelocity = body.GetPointVelocity(grabWorld);
            Vector2 force = (mouseWorldPosition - grabWorld) * Mathf.Max(0f, dragForce);
            force -= pointVelocity * Mathf.Max(0f, dragDamping);
            force = Vector2.ClampMagnitude(force, Mathf.Max(0f, maxForce));

            body.AddForceAtPosition(force, grabWorld, ForceMode2D.Force);
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
    }
}
