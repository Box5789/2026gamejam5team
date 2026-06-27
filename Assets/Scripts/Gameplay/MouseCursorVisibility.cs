using UnityEngine;

namespace KimbapGame.Gameplay
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Gameplay/Mouse Cursor Visibility")]
    public sealed class MouseCursorVisibility : MonoBehaviour
    {
        [SerializeField] private bool hideOnEnable = true;
        [SerializeField] private bool restoreOnDisable = true;
        [SerializeField] private CursorLockMode lockMode = CursorLockMode.None;

        private bool previousVisible;
        private CursorLockMode previousLockState;
        private bool hasSavedState;

        private void OnEnable()
        {
            previousVisible = Cursor.visible;
            previousLockState = Cursor.lockState;
            hasSavedState = true;

            if (hideOnEnable)
            {
                Cursor.visible = false;
                Cursor.lockState = lockMode;
            }
        }

        private void OnDisable()
        {
            if (!restoreOnDisable || !hasSavedState)
            {
                return;
            }

            Cursor.visible = previousVisible;
            Cursor.lockState = previousLockState;
            hasSavedState = false;
        }
    }
}
