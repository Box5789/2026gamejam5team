using UnityEngine;

namespace KimbapGame.Kitchen
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class KitchenIngredientSource : MonoBehaviour
    {
        [SerializeField] private KitchenIngredientDefinition definition;
        [SerializeField] private KitchenController controller;
        [SerializeField] private KitchenDropZone dropZone;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Vector2 dragPreviewSize = new Vector2(2.2f, 0.45f);
        [SerializeField] private SpriteRenderer bowlRenderer;
        [SerializeField] private SpriteRenderer ingredientRenderer;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            ApplyPlaceholderColor();
        }

        public void Configure(
            KitchenIngredientDefinition definition,
            KitchenController controller,
            KitchenDropZone dropZone,
            Camera targetCamera,
            Vector2 dragPreviewSize)
        {
            this.definition = definition;
            this.controller = controller;
            this.dropZone = dropZone;
            this.targetCamera = targetCamera;
            this.dragPreviewSize = dragPreviewSize;
            ApplyPlaceholderColor();
        }

        private void OnMouseDown()
        {
            if (definition == null)
            {
                return;
            }

            if (definition.Category == KitchenIngredientCategory.Rice)
            {
                if (controller != null)
                {
                    controller.SelectRice(definition);
                }

                return;
            }

            SpawnDragPreview();
        }

        private void SpawnDragPreview()
        {
            if (controller == null || dropZone == null)
            {
                return;
            }

            GameObject preview = definition.DragPrefab != null
                ? Instantiate(definition.DragPrefab)
                : CreatePlaceholderPreview(definition, dragPreviewSize);
            preview.name = $"{definition.DisplayName}_DragPreview";
            preview.transform.position = transform.position + new Vector3(0f, -0.45f, -1f);

            var draggable = preview.GetComponent<KitchenDraggableItem>();
            if (draggable == null)
            {
                draggable = preview.AddComponent<KitchenDraggableItem>();
            }

            draggable.Initialize(controller, dropZone, definition, targetCamera);
        }

        private void ApplyPlaceholderColor()
        {
            if (definition == null)
            {
                return;
            }

            if (ingredientRenderer != null)
            {
                ingredientRenderer.color = definition.PlaceholderColor;
            }
        }

        public static GameObject CreatePlaceholderPreview(KitchenIngredientDefinition definition, Vector2 size)
        {
            GameObject preview = new GameObject($"{definition.DisplayName}_Preview");
            var renderer = preview.AddComponent<SpriteRenderer>();
            renderer.sprite = KitchenPlaceholderFactory.CreateWhiteSprite();
            renderer.color = definition.PlaceholderColor;
            preview.transform.localScale = new Vector3(size.x, size.y, 1f);
            preview.AddComponent<BoxCollider2D>();
            return preview;
        }
    }
}
