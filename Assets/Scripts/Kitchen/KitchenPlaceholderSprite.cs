using UnityEngine;

namespace KimbapGame.Kitchen
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class KitchenPlaceholderSprite : MonoBehaviour
    {
        [SerializeField] private Color color = Color.white;
        [SerializeField] private int sortingOrder;

        private void Awake()
        {
            Apply();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Apply();
        }
#endif

        private void Apply()
        {
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                return;
            }

            if (renderer.sprite == null)
            {
                renderer.sprite = KitchenPlaceholderFactory.CreateWhiteSprite();
            }

            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
        }
    }
}
