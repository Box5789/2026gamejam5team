using UnityEngine;

namespace KimbapGame.Kitchen
{
    [DisallowMultipleComponent]
    public sealed class KitchenDraggableItem : MonoBehaviour
    {
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
                Destroy(gameObject);
                return;
            }

            if (!controller.TryAddIngredient(definition))
            {
                Destroy(gameObject);
                return;
            }

            transform.SetParent(dropZone.PlacedItemRoot, true);
            transform.position = dropZone.GetSnappedWorldPoint(transform.position);
            Collider2D itemCollider = GetComponent<Collider2D>();
            if (itemCollider != null)
            {
                itemCollider.enabled = false;
            }
        }
    }
}
