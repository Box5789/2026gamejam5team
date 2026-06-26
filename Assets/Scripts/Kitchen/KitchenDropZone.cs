using UnityEngine;

namespace KimbapGame.Kitchen
{
    [DisallowMultipleComponent]
    public sealed class KitchenDropZone : MonoBehaviour
    {
        [SerializeField] private Vector2 size = new Vector2(4.8f, 2.4f);
        [SerializeField] private Transform placedItemRoot;
        [SerializeField] private float placedItemLocalZ = -1.5f;

        public Transform PlacedItemRoot => placedItemRoot == null ? transform : placedItemRoot;

        private void Reset()
        {
            placedItemRoot = transform;
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
    }
}
