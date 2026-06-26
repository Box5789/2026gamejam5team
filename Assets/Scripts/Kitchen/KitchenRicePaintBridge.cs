using GameJam.Gameplay.Spreading;
using UnityEngine;

namespace KimbapGame.Kitchen
{
    [DisallowMultipleComponent]
    public sealed class KitchenRicePaintBridge : MonoBehaviour
    {
        [SerializeField] private SpreadableSurface surface;
        [SerializeField] private SpreadInputController inputController;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private bool disableNativeInputPolling = true;

        private KitchenController controller;
        private KitchenIngredientDefinition selectedRice;
        private bool hasRecordedSelectedRice;

        public void Configure(SpreadableSurface surface, SpreadInputController inputController, Camera targetCamera)
        {
            this.surface = surface;
            this.inputController = inputController;
            this.targetCamera = targetCamera;

            if (disableNativeInputPolling && this.inputController != null)
            {
                this.inputController.enabled = false;
            }
        }

        private void Awake()
        {
            if (surface == null)
            {
                surface = FindObjectOfType<SpreadableSurface>();
            }

            if (inputController == null)
            {
                inputController = FindObjectOfType<SpreadInputController>();
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (disableNativeInputPolling && inputController != null)
            {
                inputController.enabled = false;
            }
        }

        public void SelectRice(KitchenIngredientDefinition riceDefinition, KitchenController sourceController)
        {
            selectedRice = riceDefinition;
            controller = sourceController;
            hasRecordedSelectedRice = false;

            if (surface != null && selectedRice != null)
            {
                surface.SelectBrush(selectedRice.RiceBrushId);
            }
        }

        private void Update()
        {
            if (selectedRice == null || inputController == null || !Input.GetMouseButton(0))
            {
                return;
            }

            if (inputController.PaintScreenPoint(Input.mousePosition) && !hasRecordedSelectedRice)
            {
                if (controller != null && controller.TryAddRice(selectedRice))
                {
                    hasRecordedSelectedRice = true;
                }
            }
        }
    }
}
