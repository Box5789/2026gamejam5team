using KimbapGame.Audio;
using UnityEngine;

namespace KimbapGame.Kitchen
{
    [DisallowMultipleComponent]
    public sealed class KitchenDraggableItem : MonoBehaviour
    {
        private const int DraggingSortingOrder = 100;

        [SerializeField] private string seaweedDropSoundName = "Kitchen/Sound/김 집기";
        [SerializeField] private string fillingDropSoundName = "Kitchen/Sound/재료 놓기_mastered";
        [SerializeField] private float soundVolume = 1f;

        private KitchenController controller;
        private KitchenDropZone dropZone;
        private KitchenIngredientDefinition definition;
        private Camera targetCamera;
        private bool isDragging;

        public void Initialize(
            KitchenController controller,
            KitchenDropZone dropZone,
            KitchenIngredientDefinition definition,
            Camera targetCamera)
        {
            this.controller = controller;
            this.dropZone = dropZone;
            this.definition = definition;
            this.targetCamera = targetCamera;
            isDragging = true;
            ApplySortingOrder(DraggingSortingOrder);
        }

        private void Update()
        {
            if (!isDragging)
            {
                return;
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera != null)
            {
                Vector3 worldPoint = targetCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Mathf.Abs(targetCamera.transform.position.z)));
                worldPoint.z = -1f;
                transform.position = worldPoint;
                ApplySortingOrder(DraggingSortingOrder);
            }

            if (Input.GetMouseButtonUp(0))
            {
                Drop();
            }
        }

        private void Drop()
        {
            isDragging = false;

            if (dropZone == null || controller == null || definition == null || !dropZone.ContainsWorldPoint(transform.position))
            {
                LogRegistrationDebug($"Drop rejected before TryAddIngredient. reason='{GetDropRejectReason()}' object='{name}'");
                Destroy(gameObject);
                return;
            }

            if (!controller.TryAddIngredient(definition))
            {
                LogRegistrationDebug(
                    $"TryAddIngredient failed category='{definition.Category}' ingredient='{definition.DisplayName}' state='{controller.GetRegistrationDebugSummary(definition)}' object='{name}'");
                Destroy(gameObject);
                return;
            }

            transform.SetParent(dropZone.PlacedItemRoot, true);
            transform.position = definition.Category == KitchenIngredientCategory.Seaweed
                ? dropZone.GetCenterWorldPoint()
                : dropZone.GetSnappedWorldPoint(transform.position);
            int droppedSortingOrder = controller.RegisterDroppedObject(definition, gameObject);
            LogRegistrationDebug(
                $"Drop registered category='{definition.Category}' ingredient='{definition.DisplayName}' object='{name}' sorting={droppedSortingOrder}");
            ApplySortingOrder(droppedSortingOrder);
            PlayDropSound();

            Collider2D itemCollider = GetComponent<Collider2D>();
            if (itemCollider != null)
            {
                itemCollider.enabled = false;
            }
        }

        private void LogRegistrationDebug(string message)
        {
            if (controller != null)
            {
                controller.LogRegistrationDebug(message, this);
            }
        }

        private string GetDropRejectReason()
        {
            if (controller == null)
            {
                return "controller=null";
            }

            if (dropZone == null)
            {
                return "dropZone=null";
            }

            if (definition == null)
            {
                return "definition=null";
            }

            return dropZone.ContainsWorldPoint(transform.position) ? "unknown" : "outside-drop-zone";
        }

        private void PlayDropSound()
        {
            string resourcePath = definition.Category == KitchenIngredientCategory.Seaweed
                ? seaweedDropSoundName
                : fillingDropSoundName;
            KimbapSfxPlayer.Play(this, resourcePath, soundVolume);
        }

        private void ApplySortingOrder(int sortingOrder)
        {
            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sortingOrder = sortingOrder + i;
            }
        }
    }
}
