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
        private bool paintingEnabled;

        public bool PaintingEnabled => paintingEnabled;

        public bool HasSelectedRice => selectedRice != null;

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
            if (!paintingEnabled)
            {
                ClearSelection();
                return;
            }

            selectedRice = riceDefinition;
            controller = sourceController;
            hasRecordedSelectedRice = false;

            SpreadableSurface activeSurface = GetActiveSurface();
            if (activeSurface != null && selectedRice != null)
            {
                activeSurface.SelectOrCreateBrush(selectedRice.RiceBrushId, selectedRice.DisplayName, selectedRice.PlaceholderColor);
            }
        }

        private void Update()
        {
            if (!paintingEnabled || selectedRice == null || !Input.GetMouseButton(0))
            {
                return;
            }

            SpreadInputController activeInputController = GetActiveInputController();
            if (activeInputController == null)
            {
                return;
            }

            if (activeInputController.PaintScreenPoint(Input.mousePosition) && !hasRecordedSelectedRice)
            {
                if (controller != null && controller.TryAddRice(selectedRice))
                {
                    hasRecordedSelectedRice = true;
                }
            }
        }

        public void SetPaintingEnabled(bool enabled)
        {
            paintingEnabled = enabled;

            if (!paintingEnabled)
            {
                ClearSelection();
            }
        }

        public void ClearSelection()
        {
            selectedRice = null;
            hasRecordedSelectedRice = false;
        }

        private SpreadableSurface GetActiveSurface()
        {
            return controller != null && controller.CurrentRiceSurface != null
                ? controller.CurrentRiceSurface
                : surface;
        }

        private SpreadInputController GetActiveInputController()
        {
            return controller != null && controller.CurrentRiceInputController != null
                ? controller.CurrentRiceInputController
                : inputController;
        }
    }
}
