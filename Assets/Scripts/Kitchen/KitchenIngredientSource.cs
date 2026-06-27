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

            ApplyPlaceholderVisuals();
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
            ApplyPlaceholderVisuals();
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

            if (definition.DragPrefab == null)
            {
                Debug.LogWarning($"Kitchen ingredient '{definition.DisplayName}' is missing a drag prefab.");
                return;
            }

            GameObject preview = Instantiate(definition.DragPrefab);
            preview.name = $"{definition.DisplayName}_DragPreview";
            preview.transform.position = transform.position + new Vector3(0f, -0.45f, -1f);
            preview.transform.localScale = new Vector3(dragPreviewSize.x, dragPreviewSize.y, 1f);
            ApplyPreviewVisuals(preview);

            var draggable = preview.GetComponent<KitchenDraggableItem>();
            if (draggable == null)
            {
                Debug.LogWarning($"Drag prefab for '{definition.DisplayName}' must contain KitchenDraggableItem.");
                Destroy(preview);
                return;
            }

            draggable.Initialize(controller, dropZone, definition, targetCamera);
        }

        private void ApplyPlaceholderVisuals()
        {
            if (definition == null)
            {
                return;
            }

            if (bowlRenderer != null && bowlRenderer.sprite == null)
            {
                bowlRenderer.sprite = KitchenPlaceholderFactory.CreateWhiteSprite();
            }

            if (ingredientRenderer != null)
            {
                if (ingredientRenderer.sprite == null)
                {
                    ingredientRenderer.sprite = KitchenPlaceholderFactory.CreateWhiteSprite();
                }

                ingredientRenderer.color = definition.PlaceholderColor;
            }
        }

        private void ApplyPreviewVisuals(GameObject preview)
        {
            SpriteRenderer[] renderers = preview.GetComponentsInChildren<SpriteRenderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].sprite == null)
                {
                    renderers[i].sprite = KitchenPlaceholderFactory.CreateWhiteSprite();
                }

                renderers[i].color = definition.PlaceholderColor;
            }
        }
    }
}
