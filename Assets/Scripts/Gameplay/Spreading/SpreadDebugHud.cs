using UnityEngine;

namespace GameJam.Gameplay.Spreading
{
    [DisallowMultipleComponent]
    public sealed class SpreadDebugHud : MonoBehaviour
    {
        [SerializeField] private SpreadableSurface surface;
        [SerializeField] private KeyCode resetKey = KeyCode.R;
        [SerializeField] private bool showDebugHud = true;
        [SerializeField] private Rect hudRect = new Rect(12f, 12f, 220f, 72f);

        private void Reset()
        {
            surface = GetComponent<SpreadableSurface>();
        }

        private void Awake()
        {
            if (surface == null)
            {
                surface = GetComponent<SpreadableSurface>();
            }
        }

        private void Update()
        {
            if (surface == null || resetKey == KeyCode.None)
            {
                return;
            }

            if (Input.GetKeyDown(resetKey))
            {
                surface.ResetSpread();
            }
        }

        private void OnGUI()
        {
            if (!showDebugHud || surface == null)
            {
                return;
            }

            GUILayout.BeginArea(hudRect, GUI.skin.box);
            GUILayout.Label($"Brush: {surface.GetSelectedBrushDisplayName()}");
            GUILayout.Label($"Coverage: {surface.Coverage:P0}");
            GUILayout.Label(surface.IsComplete ? "Status: Complete" : "Status: In Progress");
            GUILayout.EndArea();
        }
    }
}
