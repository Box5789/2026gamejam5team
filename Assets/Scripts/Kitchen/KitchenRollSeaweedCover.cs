using UnityEngine;

namespace KimbapGame.Kitchen
{
    [DisallowMultipleComponent]
    public sealed class KitchenRollSeaweedCover : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer coverRenderer;

        public SpriteRenderer CoverRenderer => coverRenderer;

        public bool IsVisible => coverRenderer != null && coverRenderer.gameObject.activeSelf;

        public void Show(SpriteRenderer sourceRenderer, float progress, int sortingOrder)
        {
            if (sourceRenderer == null || coverRenderer == null)
            {
                return;
            }

            float clampedProgress = Mathf.Clamp01(progress);
            if (clampedProgress <= 0f)
            {
                Hide();
                return;
            }

            CopyRendererState(sourceRenderer, sortingOrder);
            Transform coverTransform = coverRenderer.transform;
            Bounds sourceBounds = sourceRenderer.sprite == null
                ? new Bounds(Vector3.zero, Vector3.one)
                : sourceRenderer.sprite.bounds;
            float height = sourceBounds.size.y * clampedProgress;
            coverTransform.localPosition = new Vector3(
                sourceBounds.center.x,
                sourceBounds.min.y + (height * 0.5f),
                0f);
            coverTransform.localRotation = Quaternion.identity;
            coverTransform.localScale = new Vector3(1f, clampedProgress, 1f);
            coverRenderer.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (coverRenderer != null)
            {
                coverRenderer.gameObject.SetActive(false);
            }
        }

        private void CopyRendererState(SpriteRenderer sourceRenderer, int sortingOrder)
        {
            coverRenderer.sprite = sourceRenderer.sprite;
            coverRenderer.color = sourceRenderer.color;
            coverRenderer.flipX = sourceRenderer.flipX;
            coverRenderer.flipY = sourceRenderer.flipY;
            coverRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
            coverRenderer.sortingOrder = sortingOrder;
            coverRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
        }
    }
}
