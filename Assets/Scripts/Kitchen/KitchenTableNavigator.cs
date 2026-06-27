using System;
using UnityEngine;
using UnityEngine.UI;

namespace KimbapGame.Kitchen
{
    [DisallowMultipleComponent]
    public sealed class KitchenTableNavigator : MonoBehaviour
    {
        [SerializeField] private Transform movingTablesRoot;
        [SerializeField] private Button nextButton;
        [SerializeField] private KitchenRicePaintBridge ricePaintBridge;
        [SerializeField] private Transform ricePaintingTableRoot;
        [SerializeField] private int ricePaintingTableIndex = 1;
        [SerializeField] private float slideDuration = 0.35f;

        private int currentTableIndex;
        private Vector3 startPosition;
        private Vector3 slideStartPosition;
        private Vector3 slideTargetPosition;
        private float slideTimer;
        private bool isSliding;

        public event Action<int> TableChanged;

        public int CurrentTableIndex => currentTableIndex;

        public int TableCount => GetTableCount();

        public Transform CurrentTableRoot => GetTableRoot(currentTableIndex);

        public void Configure(Transform movingTablesRoot, Button nextButton)
        {
            this.movingTablesRoot = movingTablesRoot;
            this.nextButton = nextButton;
            startPosition = this.movingTablesRoot == null ? Vector3.zero : this.movingTablesRoot.localPosition;
            currentTableIndex = ClampTableIndex(currentTableIndex, TableCount);
            RegisterButtonListener();
            UpdateRicePaintingMode();
        }

        public void Configure(Transform movingTablesRoot, Button nextButton, float tableSpacing, int tableCount)
        {
            Configure(movingTablesRoot, nextButton);
        }

        private void Awake()
        {
            if (movingTablesRoot != null)
            {
                startPosition = movingTablesRoot.localPosition;
            }

            currentTableIndex = ClampTableIndex(currentTableIndex, TableCount);
            RegisterButtonListener();
            UpdateRicePaintingMode();
        }

        private void OnDestroy()
        {
            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(GoToNextTable);
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
            int clampedIndex = ClampTableIndex(tableIndex, TableCount);
            if (clampedIndex == currentTableIndex && !isSliding)
            {
                return;
            }

            currentTableIndex = clampedIndex;
            BeginSlide();
            UpdateRicePaintingMode();
            TableChanged?.Invoke(currentTableIndex);
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
            slideTargetPosition = GetTargetRootPosition(currentTableIndex);
            slideTimer = 0f;
            isSliding = true;

            if (slideDuration <= 0f)
            {
                isSliding = false;
                movingTablesRoot.localPosition = slideTargetPosition;
            }
        }

        private void UpdateRicePaintingMode()
        {
            if (ricePaintBridge != null)
            {
                ricePaintBridge.SetPaintingEnabled(IsCurrentRicePaintingTable());
            }
        }

        private bool IsCurrentRicePaintingTable()
        {
            Transform currentTableRoot = CurrentTableRoot;
            if (ricePaintingTableRoot != null && currentTableRoot != null)
            {
                return currentTableRoot == ricePaintingTableRoot;
            }

            return currentTableIndex == ricePaintingTableIndex;
        }

        private Vector3 GetTargetRootPosition(int tableIndex)
        {
            Transform firstTableRoot = GetTableRoot(0);
            Transform selectedTableRoot = GetTableRoot(tableIndex);
            if (firstTableRoot == null || selectedTableRoot == null)
            {
                return startPosition;
            }

            Vector3 tableOffset = selectedTableRoot.localPosition - firstTableRoot.localPosition;
            tableOffset.z = 0f;
            Vector3 targetPosition = startPosition - tableOffset;
            targetPosition.z = startPosition.z;
            return targetPosition;
        }

        private Transform GetTableRoot(int index)
        {
            if (movingTablesRoot == null || index < 0 || index >= movingTablesRoot.childCount)
            {
                return null;
            }

            return movingTablesRoot.GetChild(index);
        }

        private int GetTableCount()
        {
            return movingTablesRoot == null ? 0 : movingTablesRoot.childCount;
        }

        private void RegisterButtonListener()
        {
            if (nextButton == null)
            {
                return;
            }

            nextButton.onClick.RemoveListener(GoToNextTable);
            nextButton.onClick.AddListener(GoToNextTable);
        }
    }
}
