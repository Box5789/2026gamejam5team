using UnityEngine;
using UnityEngine.UI;

namespace KimbapGame.Kitchen
{
    [DisallowMultipleComponent]
    public sealed class KitchenTableNavigator : MonoBehaviour
    {
        [SerializeField] private Transform movingTablesRoot;
        [SerializeField] private Button nextButton;
        [SerializeField] private float tableSpacing = 9f;
        [SerializeField] private int tableCount = 3;
        [SerializeField] private float slideDuration = 0.35f;

        private int currentTableIndex;
        private Vector3 startPosition;
        private Vector3 slideStartPosition;
        private Vector3 slideTargetPosition;
        private float slideTimer;
        private bool isSliding;

        public int CurrentTableIndex => currentTableIndex;

        public int TableCount => tableCount;

        public void Configure(Transform movingTablesRoot, Button nextButton, float tableSpacing, int tableCount)
        {
            this.movingTablesRoot = movingTablesRoot;
            this.nextButton = nextButton;
            this.tableSpacing = tableSpacing;
            this.tableCount = Mathf.Max(1, tableCount);
            startPosition = this.movingTablesRoot == null ? Vector3.zero : this.movingTablesRoot.localPosition;

            if (this.nextButton != null)
            {
                this.nextButton.onClick.AddListener(GoToNextTable);
            }
        }

        private void Awake()
        {
            if (movingTablesRoot != null)
            {
                startPosition = movingTablesRoot.localPosition;
            }

            if (nextButton != null)
            {
                nextButton.onClick.AddListener(GoToNextTable);
            }
        }

        private void Update()
        {
            if (!isSliding || movingTablesRoot == null)
            {
                return;
            }

            slideTimer += Time.deltaTime;
            float t = slideDuration <= 0f ? 1f : Mathf.Clamp01(slideTimer / slideDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            movingTablesRoot.localPosition = Vector3.Lerp(slideStartPosition, slideTargetPosition, eased);

            if (t >= 1f)
            {
                isSliding = false;
                movingTablesRoot.localPosition = slideTargetPosition;
            }
        }

        public void GoToNextTable()
        {
            SetTableIndex(currentTableIndex + 1);
        }

        public void SetTableIndex(int tableIndex)
        {
            int clampedIndex = ClampTableIndex(tableIndex, tableCount);
            if (clampedIndex == currentTableIndex && !isSliding)
            {
                return;
            }

            currentTableIndex = clampedIndex;
            BeginSlide();
        }

        public static int ClampTableIndex(int tableIndex, int tableCount)
        {
            if (tableCount <= 0)
            {
                return 0;
            }

            return Mathf.Clamp(tableIndex, 0, tableCount - 1);
        }

        private void BeginSlide()
        {
            if (movingTablesRoot == null)
            {
                return;
            }

            slideStartPosition = movingTablesRoot.localPosition;
            slideTargetPosition = startPosition + Vector3.left * tableSpacing * currentTableIndex;
            slideTimer = 0f;
            isSliding = true;
        }
    }
}
