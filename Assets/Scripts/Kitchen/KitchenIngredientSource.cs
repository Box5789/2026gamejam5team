using KimbapGame.Audio;
using UnityEngine;

namespace KimbapGame.Kitchen
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class KitchenIngredientSource : MonoBehaviour
    {
        private const float MinimumPickupColliderSize = 0.01f;

        [SerializeField] public KitchenIngredientDefinition definition;
        [SerializeField] private KitchenController controller;
        [SerializeField] private KitchenDropZone dropZone;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Vector2 dragPreviewSize = new Vector2(2.2f, 0.45f);
        [SerializeField] private SpriteRenderer bowlRenderer;
        [SerializeField] private SpriteRenderer ingredientRenderer;
        [SerializeField] private string seaweedPickupSoundName = "Kitchen/Sound/김 집기";
        [SerializeField] private string fillingPickupSoundName = "Kitchen/Sound/재료 픽_mastered";
        [SerializeField] private string riceSelectSoundName = "Kitchen/Sound/밥 선택_mastered";
        [SerializeField] private float soundVolume = 1f;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            ApplyPlaceholderVisuals();
            AlignPickupColliderToIngredientRenderer();
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
            AlignPickupColliderToIngredientRenderer();
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
                    KimbapSfxPlayer.Play(this, riceSelectSoundName, soundVolume);
                }

                return;
            }

            if (SpawnDragPreview())
            {
                PlayPickupSound();
            }
        }

        private bool SpawnDragPreview()
        {
            if (controller == null || dropZone == null)
            {
                return false;
            }

            if (definition.DragPrefab == null)
            {
                Debug.LogWarning($"Kitchen ingredient '{definition.DisplayName}' is missing a drag prefab.");
                return false;
            }

            GameObject preview = Instantiate(definition.DragPrefab);
            preview.name = $"{definition.DisplayName}_DragPreview";
            preview.transform.position = GetPickupWorldCenterWithPreviewDepth();
            preview.transform.localScale = new Vector3(dragPreviewSize.x, dragPreviewSize.y, 1f);
            ApplyPreviewVisuals(preview);

            var draggable = preview.GetComponent<KitchenDraggableItem>();
            if (draggable == null)
            {
                Debug.LogWarning($"Drag prefab for '{definition.DisplayName}' must contain KitchenDraggableItem.");
                Destroy(preview);
                return false;
            }

            draggable.Initialize(controller, dropZone, definition, targetCamera);
            return true;
        }

        private void PlayPickupSound()
        {
            string resourcePath = definition.Category == KitchenIngredientCategory.Seaweed
                ? seaweedPickupSoundName
                : fillingPickupSoundName;
            KimbapSfxPlayer.Play(this, resourcePath, soundVolume);
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
                if (definition.VisualSprite != null)
                {
                    ingredientRenderer.sprite = definition.VisualSprite;
                    ingredientRenderer.color = Color.white;
                    return;
                }

                if (ingredientRenderer.sprite == null)
                {
                    ingredientRenderer.sprite = KitchenPlaceholderFactory.CreateWhiteSprite();
                }

                ingredientRenderer.color = definition.PlaceholderColor;
            }
        }

        private void AlignPickupColliderToIngredientRenderer()
        {
            var pickupCollider = GetComponent<BoxCollider2D>();
            if (pickupCollider == null || ingredientRenderer == null || ingredientRenderer.sprite == null)
            {
                return;
            }

            Bounds sourceLocalBounds = GetRendererBoundsInSourceLocal(ingredientRenderer);
            Vector3 size = sourceLocalBounds.size;
            if (size.x <= 0f || size.y <= 0f)
            {
                return;
            }

            pickupCollider.offset = new Vector2(sourceLocalBounds.center.x, sourceLocalBounds.center.y);
            pickupCollider.size = new Vector2(
                Mathf.Max(size.x, MinimumPickupColliderSize),
                Mathf.Max(size.y, MinimumPickupColliderSize));
        }

        private Bounds GetRendererBoundsInSourceLocal(SpriteRenderer renderer)
        {
            Bounds spriteBounds = renderer.sprite.bounds;
            Vector3 min = spriteBounds.min;
            Vector3 max = spriteBounds.max;
            if (renderer.flipX)
            {
                float originalMinX = min.x;
                min.x = -max.x;
                max.x = -originalMinX;
            }

            if (renderer.flipY)
            {
                float originalMinY = min.y;
                min.y = -max.y;
                max.y = -originalMinY;
            }

            Vector3 firstPoint = TransformRendererLocalPoint(renderer.transform, new Vector3(min.x, min.y, 0f));
            Bounds bounds = new Bounds(firstPoint, Vector3.zero);
            bounds.Encapsulate(TransformRendererLocalPoint(renderer.transform, new Vector3(min.x, max.y, 0f)));
            bounds.Encapsulate(TransformRendererLocalPoint(renderer.transform, new Vector3(max.x, min.y, 0f)));
            bounds.Encapsulate(TransformRendererLocalPoint(renderer.transform, new Vector3(max.x, max.y, 0f)));
            return bounds;
        }

        private Vector3 TransformRendererLocalPoint(Transform rendererTransform, Vector3 rendererLocalPoint)
        {
            return transform.InverseTransformPoint(rendererTransform.TransformPoint(rendererLocalPoint));
        }

        private Vector3 GetPickupWorldCenterWithPreviewDepth()
        {
            Vector3 position = ingredientRenderer != null && ingredientRenderer.sprite != null
                ? ingredientRenderer.bounds.center
                : transform.position;
            position.z = transform.position.z - 1f;
            return position;
        }

        private void ApplyPreviewVisuals(GameObject preview)
        {
            Sprite previewSprite = definition.DragPreviewSprite != null
                ? definition.DragPreviewSprite
                : definition.VisualSprite;
            SpriteRenderer[] renderers = preview.GetComponentsInChildren<SpriteRenderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                if (previewSprite != null)
                {
                    renderers[i].sprite = previewSprite;
                    renderers[i].color = Color.white;
                    continue;
                }

                if (renderers[i].sprite == null)
                {
                    renderers[i].sprite = KitchenPlaceholderFactory.CreateWhiteSprite();
                }

                renderers[i].color = definition.PlaceholderColor;
            }
        }
    }
}
