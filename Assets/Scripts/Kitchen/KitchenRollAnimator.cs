using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KimbapGame.Kitchen
{
    [DisallowMultipleComponent]
    public sealed class KitchenRollAnimator : MonoBehaviour
    {
        private const int FinalFillingSortingOrder = 158;
        private const int FinalSeaweedSortingOrder = 160;
        private const float MinimumScale = 0.001f;

        [SerializeField] private KitchenController controller;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Button completeButton;
        [SerializeField] private Button submitButton;
        [SerializeField, Range(0.01f, 1f)] private float dragStartBottomRatio = 0.25f;
        [SerializeField, Min(0.01f)] private float dragDistanceToFullRoll = 2f;
        [SerializeField, Min(0f)] private float unrollSpeed = 1f;
        [SerializeField, Range(0.01f, 1f)] private float completeProgressThreshold = 0.95f;
        [SerializeField] private Vector2 finalSeaweedSizeRatio = new Vector2(0.92f, 0.5f);
        [SerializeField, Range(0.01f, 1f)] private float finalFillingYScale = 0.5f;

        private readonly List<FillingPose> fillings = new List<FillingPose>();
        private Transform seaweedTransform;
        private SpriteRenderer seaweedRenderer;
        private KitchenRollSeaweedCover seaweedCover;
        private Vector3 originalSeaweedPosition;
        private Quaternion originalSeaweedRotation;
        private Vector3 originalSeaweedScale;
        private Bounds originalSeaweedBounds;
        private int originalSeaweedSortingOrder;
        private float rollProgress;
        private float dragStartWorldY;
        private float dragStartProgress;
        private bool isDragging;
        private bool hasSnapshot;
        private bool hasFinalized;

        public bool IsRolling => isDragging;

        public float RollProgress => rollProgress;

        public bool IsReadyToComplete => rollProgress >= completeProgressThreshold;

        public void Configure(KitchenController controller)
        {
            this.controller = controller;
        }

        public void ConfigureTuningForTests(
            float dragStartBottomRatio,
            float dragDistanceToFullRoll,
            float unrollSpeed,
            float completeProgressThreshold,
            float finalFillingYScale = 0.5f)
        {
            this.dragStartBottomRatio = Mathf.Clamp01(dragStartBottomRatio);
            this.dragDistanceToFullRoll = Mathf.Max(0.01f, dragDistanceToFullRoll);
            this.unrollSpeed = Mathf.Max(0f, unrollSpeed);
            this.completeProgressThreshold = Mathf.Clamp01(completeProgressThreshold);
            this.finalFillingYScale = Mathf.Clamp(finalFillingYScale, 0.01f, 1f);
        }

        public bool BeginRollDragForTests(Vector2 screenPosition)
        {
            return TryBeginDrag(screenPosition);
        }

        public void DragRollForTests(Vector2 screenPosition)
        {
            if (isDragging)
            {
                SetProgressFromDrag(screenPosition);
            }
        }

        public void ReleaseRollForTests()
        {
            isDragging = false;
        }

        public void TickForTests(float deltaTime)
        {
            TickUnroll(deltaTime);
        }

        public void SetRollProgressForTests(float progress)
        {
            SetProgress(progress);
        }

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            SetCompleteButtonAvailable(false);
        }

        private void OnEnable()
        {
            if (completeButton != null)
            {
                completeButton.onClick.AddListener(HandleCompleteClicked);
            }
        }

        private void OnDisable()
        {
            if (completeButton != null)
            {
                completeButton.onClick.RemoveListener(HandleCompleteClicked);
            }
        }

        private void Update()
        {
            if (hasFinalized)
            {
                return;
            }

            TickPointerInput();
            TickUnroll(Time.deltaTime);
            SetCompleteButtonAvailable(IsReadyToComplete);
        }

        public void FinalizeRoll()
        {
            if (hasFinalized || !IsReadyToComplete || !CaptureSnapshot())
            {
                return;
            }

            isDragging = false;
            hasFinalized = true;
            SetProgress(1f);
            HideRiceSurface();
            ApplyFinalSorting();
            SetCompleteButtonAvailable(false);
        }

        private void HandleCompleteClicked()
        {
            FinalizeRoll();
            if (!hasFinalized)
            {
                return;
            }

            if (controller != null)
            {
                controller.CompleteAndSave();
            }

            if (submitButton != null)
            {
                submitButton.gameObject.SetActive(true);
            }
        }

        private void TickPointerInput()
        {
            if (!TryGetPointer(out Vector2 screenPosition, out bool pressed, out bool started, out bool ended))
            {
                isDragging = false;
                return;
            }

            if (started && !isDragging)
            {
                TryBeginDrag(screenPosition);
            }

            if (isDragging && pressed)
            {
                SetProgressFromDrag(screenPosition);
            }

            if (ended || !pressed)
            {
                isDragging = false;
            }
        }

        private bool TryBeginDrag(Vector2 screenPosition)
        {
            if (hasFinalized || !CaptureSnapshot())
            {
                return false;
            }

            Vector3 worldPoint = ScreenToWorld(screenPosition);
            if (!IsInStartArea(worldPoint))
            {
                return false;
            }

            isDragging = true;
            dragStartWorldY = worldPoint.y;
            dragStartProgress = rollProgress;
            SetProgressFromDrag(screenPosition);
            return true;
        }

        private void SetProgressFromDrag(Vector2 screenPosition)
        {
            float deltaY = ScreenToWorld(screenPosition).y - dragStartWorldY;
            SetProgress(dragStartProgress + (deltaY / dragDistanceToFullRoll));
        }

        private void TickUnroll(float deltaTime)
        {
            if (isDragging || hasFinalized || IsReadyToComplete || rollProgress <= 0f)
            {
                return;
            }

            SetProgress(rollProgress - (unrollSpeed * Mathf.Max(0f, deltaTime)));
        }

        private void SetProgress(float progress)
        {
            if (!CaptureSnapshot())
            {
                return;
            }

            rollProgress = Mathf.Clamp01(progress);
            if (rollProgress <= 0f)
            {
                RestoreFlatPose();
            }
            else
            {
                ApplyRolledPose(rollProgress);
            }

            SetCompleteButtonAvailable(IsReadyToComplete && !hasFinalized);
        }

        private bool CaptureSnapshot()
        {
            if (controller == null || controller.TopSeaweedObject == null)
            {
                if (controller != null)
                {
                    controller.LogRegistrationDebug("Roll snapshot skipped because TopSeaweedObject is null.", this);
                }

                return false;
            }

            Transform currentSeaweed = controller.TopSeaweedObject.transform;
            bool shouldCaptureSeaweed = !hasSnapshot || seaweedTransform != currentSeaweed;
            if (shouldCaptureSeaweed && !CaptureSeaweedSnapshot(currentSeaweed))
            {
                return false;
            }

            int droppedFillingCount = controller.DroppedFillingObjects.Count;
            int skippedNullFillings = RefreshFillingSnapshot();
            controller.LogRegistrationDebug(
                $"Roll snapshot topSeaweed='{seaweedTransform.name}' DroppedFillingObjects count={droppedFillingCount} snapshotFillingCount={fillings.Count} skippedNullFillings={skippedNullFillings}",
                this);
            hasSnapshot = true;
            return true;
        }

        private bool CaptureSeaweedSnapshot(Transform currentSeaweed)
        {
            seaweedTransform = currentSeaweed;
            seaweedRenderer = seaweedTransform.GetComponent<SpriteRenderer>();
            if (seaweedRenderer == null)
            {
                seaweedRenderer = seaweedTransform.GetComponentInChildren<SpriteRenderer>();
            }

            if (seaweedRenderer == null)
            {
                return false;
            }

            originalSeaweedPosition = seaweedTransform.position;
            originalSeaweedRotation = seaweedTransform.rotation;
            originalSeaweedScale = seaweedTransform.localScale;
            originalSeaweedBounds = seaweedRenderer.bounds;
            originalSeaweedSortingOrder = seaweedRenderer.sortingOrder;
            seaweedCover = seaweedTransform.GetComponent<KitchenRollSeaweedCover>();
            if (seaweedCover != null)
            {
                seaweedCover.Hide();
            }

            rollProgress = 0f;
            hasFinalized = false;
            return true;
        }

        private int RefreshFillingSnapshot()
        {
            IReadOnlyList<GameObject> droppedFillings = controller.DroppedFillingObjects;
            if (FillingSnapshotMatches(droppedFillings, out int skippedNullFillings))
            {
                return skippedNullFillings;
            }

            fillings.Clear();
            skippedNullFillings = 0;
            for (int i = 0; i < droppedFillings.Count; i++)
            {
                if (droppedFillings[i] != null)
                {
                    fillings.Add(new FillingPose(droppedFillings[i].transform));
                }
                else
                {
                    skippedNullFillings++;
                }
            }

            return skippedNullFillings;
        }

        private bool FillingSnapshotMatches(IReadOnlyList<GameObject> droppedFillings, out int skippedNullFillings)
        {
            skippedNullFillings = 0;
            int fillingIndex = 0;
            for (int i = 0; i < droppedFillings.Count; i++)
            {
                GameObject droppedFilling = droppedFillings[i];
                if (droppedFilling == null)
                {
                    skippedNullFillings++;
                    continue;
                }

                if (fillingIndex >= fillings.Count || fillings[fillingIndex].Transform != droppedFilling.transform)
                {
                    return false;
                }

                fillingIndex++;
            }

            return fillingIndex == fillings.Count;
        }

        private void ApplyRolledPose(float progress)
        {
            float clampedProgress = Mathf.Clamp01(progress);
            float finalXRatio = Mathf.Max(MinimumScale, finalSeaweedSizeRatio.x);
            float finalYRatio = Mathf.Max(MinimumScale, finalSeaweedSizeRatio.y);
            float currentYRatio = Mathf.Lerp(1f, finalYRatio, clampedProgress);

            seaweedTransform.position = originalSeaweedPosition
                + Vector3.up * (originalSeaweedBounds.size.y * (1f - currentYRatio) * 0.5f);
            seaweedTransform.rotation = originalSeaweedRotation;
            seaweedTransform.localScale = new Vector3(
                originalSeaweedScale.x * Mathf.Lerp(1f, finalXRatio, clampedProgress),
                originalSeaweedScale.y * currentYRatio,
                originalSeaweedScale.z);

            float finalCenterY = originalSeaweedBounds.center.y
                + (originalSeaweedBounds.size.y * (1f - finalYRatio) * 0.5f);
            for (int i = 0; i < fillings.Count; i++)
            {
                fillings[i].ApplyYCompression(
                    originalSeaweedBounds.center.y,
                    finalCenterY,
                    finalFillingYScale,
                    clampedProgress);
            }

            ShowSeaweedCover(clampedProgress);
        }

        private void RestoreFlatPose()
        {
            if (seaweedTransform != null)
            {
                seaweedTransform.position = originalSeaweedPosition;
                seaweedTransform.rotation = originalSeaweedRotation;
                seaweedTransform.localScale = originalSeaweedScale;
            }

            if (seaweedRenderer != null)
            {
                seaweedRenderer.sortingOrder = originalSeaweedSortingOrder;
            }

            if (seaweedCover != null)
            {
                seaweedCover.Hide();
            }

            for (int i = 0; i < fillings.Count; i++)
            {
                fillings[i].Restore();
            }
        }

        private void ApplyFinalSorting()
        {
            if (seaweedRenderer != null)
            {
                seaweedRenderer.sortingOrder = FinalSeaweedSortingOrder;
            }

            for (int i = 0; i < fillings.Count; i++)
            {
                SetSortingOrder(fillings[i].Transform, FinalFillingSortingOrder + i);
            }

            ShowSeaweedCover(1f);
        }

        private void ShowSeaweedCover(float progress)
        {
            if (seaweedCover == null)
            {
                return;
            }

            if (progress <= 0f)
            {
                seaweedCover.Hide();
                return;
            }

            seaweedCover.Show(seaweedRenderer, progress, ResolveSeaweedCoverSortingOrder());
        }

        private int ResolveSeaweedCoverSortingOrder()
        {
            int maxSortingOrder = seaweedRenderer == null
                ? originalSeaweedSortingOrder
                : seaweedRenderer.sortingOrder;

            for (int i = 0; i < fillings.Count; i++)
            {
                maxSortingOrder = MaxSortingOrder(maxSortingOrder, fillings[i].Transform);
            }

            if (controller != null && controller.CurrentRiceSurface != null)
            {
                maxSortingOrder = MaxSortingOrder(maxSortingOrder, controller.CurrentRiceSurface.transform);
            }

            return maxSortingOrder + 1;
        }

        private void HideRiceSurface()
        {
            if (controller != null && controller.CurrentRiceSurface != null)
            {
                controller.CurrentRiceSurface.gameObject.SetActive(false);
            }
        }

        private bool IsInStartArea(Vector3 worldPoint)
        {
            float bottomLimit = originalSeaweedBounds.min.y
                + (originalSeaweedBounds.size.y * Mathf.Clamp01(dragStartBottomRatio));
            return worldPoint.x >= originalSeaweedBounds.min.x
                && worldPoint.x <= originalSeaweedBounds.max.x
                && worldPoint.y >= originalSeaweedBounds.min.y
                && worldPoint.y <= bottomLimit;
        }

        private Vector3 ScreenToWorld(Vector2 screenPosition)
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera == null)
            {
                return new Vector3(screenPosition.x, screenPosition.y, originalSeaweedPosition.z);
            }

            float distance = Mathf.Abs(targetCamera.transform.position.z - originalSeaweedPosition.z);
            return targetCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, distance));
        }

        private bool TryGetPointer(out Vector2 screenPosition, out bool pressed, out bool started, out bool ended)
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                screenPosition = touch.position;
                started = touch.phase == TouchPhase.Began;
                ended = touch.phase == TouchPhase.Canceled || touch.phase == TouchPhase.Ended;
                pressed = !ended;
                return true;
            }

            screenPosition = Input.mousePosition;
            pressed = Input.GetMouseButton(0);
            started = Input.GetMouseButtonDown(0);
            ended = Input.GetMouseButtonUp(0);
            return pressed || started || ended || isDragging;
        }

        private void SetCompleteButtonAvailable(bool available)
        {
            if (completeButton == null)
            {
                return;
            }

            completeButton.gameObject.SetActive(available);
            completeButton.interactable = available;
        }

        private static int MaxSortingOrder(int currentMax, Transform target)
        {
            if (target == null)
            {
                return currentMax;
            }

            SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                currentMax = Mathf.Max(currentMax, renderers[i].sortingOrder);
            }

            return currentMax;
        }

        private static void SetSortingOrder(Transform target, int sortingOrder)
        {
            if (target == null)
            {
                return;
            }

            SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sortingOrder = sortingOrder + i;
            }
        }

        private readonly struct FillingPose
        {
            private readonly Vector3 originalPosition;
            private readonly Quaternion originalRotation;
            private readonly Vector3 originalScale;

            public FillingPose(Transform transform)
            {
                Transform = transform;
                originalPosition = transform.position;
                originalRotation = transform.rotation;
                originalScale = transform.localScale;
            }

            public Transform Transform { get; }

            public void ApplyYCompression(
                float originalSeaweedCenterY,
                float finalSeaweedCenterY,
                float finalFillingYScale,
                float progress)
            {
                if (Transform == null)
                {
                    return;
                }

                float compressedY = finalSeaweedCenterY
                    + ((originalPosition.y - originalSeaweedCenterY) * finalFillingYScale);
                Vector3 position = originalPosition;
                position.y = Mathf.Lerp(originalPosition.y, compressedY, Mathf.Clamp01(progress));
                Transform.position = position;
                Transform.rotation = originalRotation;
                Transform.localScale = originalScale;
            }

            public void Restore()
            {
                if (Transform == null)
                {
                    return;
                }

                Transform.position = originalPosition;
                Transform.rotation = originalRotation;
                Transform.localScale = originalScale;
            }
        }
    }
}
