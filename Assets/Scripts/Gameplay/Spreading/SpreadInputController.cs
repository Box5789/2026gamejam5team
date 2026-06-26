using UnityEngine;

namespace GameJam.Gameplay.Spreading
{
    [DisallowMultipleComponent]
    public sealed class SpreadInputController : MonoBehaviour
    {
        [SerializeField] private SpreadableSurface surface;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private bool enableMouseInput = true;
        [SerializeField] private bool enableTouchInput = true;

        private void Reset()
        {
            surface = GetComponent<SpreadableSurface>();
            targetCamera = Camera.main;
        }

        private void Awake()
        {
            if (surface == null)
            {
                surface = GetComponent<SpreadableSurface>();
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (surface == null)
            {
                return;
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera == null)
            {
                return;
            }

            if (enableMouseInput && Input.GetMouseButton(0))
            {
                PaintScreenPoint(Input.mousePosition);
            }

            if (!enableTouchInput)
            {
                return;
            }

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    PaintScreenPoint(touch.position);
                }
            }
        }

        public bool PaintScreenPoint(Vector2 screenPoint)
        {
            Vector3 worldPoint = ScreenToSurfaceWorldPoint(screenPoint);
            return surface.PaintAtWorldPoint(worldPoint);
        }

        private Vector3 ScreenToSurfaceWorldPoint(Vector2 screenPoint)
        {
            float distanceToSurface = Mathf.Abs(targetCamera.transform.position.z - surface.transform.position.z);
            return targetCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, distanceToSurface));
        }
    }
}
