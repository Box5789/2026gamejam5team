using KimbapGame.Gameplay;
using UnityEngine;

namespace KimbapGame.Kitchen
{
    [DisallowMultipleComponent]
    public sealed class KitchenDropZone : MonoBehaviour
    {
        [SerializeField] private Vector2 size = new Vector2(4.8f, 2.4f);
        [SerializeField] private Transform placedItemRoot;
        [SerializeField] private float placedItemLocalZ = -1.5f;
        [SerializeField] private BoxCollider2D rollPickupCollider;
        [SerializeField] private Rigidbody2D rollRigidbody;
        [SerializeField] private MouseThrow2D rollMouseThrow;

        private bool throwableRollPrepared;

        public Transform PlacedItemRoot => placedItemRoot == null ? transform : placedItemRoot;

        public bool IsThrowableRollPrepared => throwableRollPrepared;

        private void Reset()
        {
            placedItemRoot = transform;
            CacheThrowableComponents();
        }

        private void Awake()
        {
            CacheThrowableComponents();
        }

        public bool ContainsWorldPoint(Vector3 worldPoint)
        {
            Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
            return Mathf.Abs(localPoint.x) <= size.x * 0.5f
                && Mathf.Abs(localPoint.y) <= size.y * 0.5f;
        }

        public Vector3 GetSnappedWorldPoint(Vector3 worldPoint)
        {
            Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
            localPoint.x = Mathf.Clamp(localPoint.x, -size.x * 0.5f, size.x * 0.5f);
            localPoint.y = Mathf.Clamp(localPoint.y, -size.y * 0.5f, size.y * 0.5f);
            localPoint.z = placedItemLocalZ;
            return transform.TransformPoint(localPoint);
        }

        public Vector3 GetCenterWorldPoint()
        {
            return transform.TransformPoint(new Vector3(0f, 0f, placedItemLocalZ));
        }

        public void PrepareThrowableRoll()
        {
            CacheThrowableComponents();
            throwableRollPrepared = true;

            if (rollPickupCollider != null)
            {
                rollPickupCollider.enabled = true;
            }

            if (rollRigidbody != null)
            {
                rollRigidbody.bodyType = RigidbodyType2D.Kinematic;
            }

            if (rollMouseThrow != null)
            {
                rollMouseThrow.enabled = false;
            }
        }

        private void OnMouseDown()
        {
            ActivateThrowableRollFromCurrentMouse();
        }

        public bool ActivateThrowableRollFromCurrentMouse()
        {
            if (!throwableRollPrepared)
            {
                return false;
            }

            CacheThrowableComponents();
            if (rollRigidbody != null)
            {
                rollRigidbody.bodyType = RigidbodyType2D.Dynamic;
            }

            if (rollMouseThrow != null)
            {
                rollMouseThrow.enabled = true;
                return rollMouseThrow.BeginDragFromCurrentMouse();
            }

            return false;
        }

        private void CacheThrowableComponents()
        {
            if (rollPickupCollider == null)
            {
                rollPickupCollider = GetComponent<BoxCollider2D>();
            }

            if (rollRigidbody == null)
            {
                rollRigidbody = GetComponent<Rigidbody2D>();
            }

            if (rollMouseThrow == null)
            {
                rollMouseThrow = GetComponent<MouseThrow2D>();
            }
        }
    }
}
