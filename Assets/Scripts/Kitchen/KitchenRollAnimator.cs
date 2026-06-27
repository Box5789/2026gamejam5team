using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KimbapGame.Kitchen
{
    [DisallowMultipleComponent]
    public sealed class KitchenRollAnimator : MonoBehaviour
    {
        private const int FillingStripSortingOrder = 158;
        private const int BodySortingOrder = 160;
        private const int CapturedFillingSortingBase = 190;
        private const int RollSortingOrder = 220;
        private const float MinimumVisibleScale = 0.001f;

        [SerializeField] private KitchenController controller;
        [SerializeField] private GameObject rollGuidePrefab;
        [SerializeField] private GameObject completedKimbapPrefab;
        [SerializeField] private GameObject completedFillingCapPrefab;
        [SerializeField] private Button rollButton;
        [SerializeField] private Button completeButton;
        [SerializeField] private Button submitButton;
        [SerializeField] private float growDuration = 0.45f;
        [SerializeField] private float moveDuration = 0.8f;
        [SerializeField] private Color rollColor = new Color(1f, 0.78f, 0.18f, 1f);
        [SerializeField] private Color completedOuterColor = new Color(0.08f, 0.14f, 0.09f, 1f);
        [SerializeField] private Vector3 completedPreviewLocalOffset = Vector3.zero;
        [SerializeField] private Vector2 completedBodySizeRatio = new Vector2(0.92f, 0.36f);
        [SerializeField] private float completedFillingWidthOffset = 0.1f;
        [SerializeField] private float completedFillingVerticalInset = 0.05f;
        [SerializeField] private float completedFillingHeightScale = 0.72f;

        private readonly List<FillingRollState> fillingStates = new List<FillingRollState>();
        private GameObject rollObject;
        private SpriteRenderer rollRenderer;
        private GameObject completedKimbapObject;
        private Transform seaweedTransform;
        private SpriteRenderer seaweedRenderer;
        private Transform visualParent;
        private Vector3 originalSeaweedScale;
        private Vector3 originalSeaweedPosition;
        private Bounds originalSeaweedBounds;
        private int capturedFillingSlotCount;
        private bool isRolling;
        private bool hasPreparedRoll;

        public bool IsRolling => isRolling;

        public bool HasRollVisual => rollObject != null;

        public bool HasCompletedKimbap => completedKimbapObject != null;

        public int CapturedFillingCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < fillingStates.Count; i++)
                {
                    if (fillingStates[i].IsCaptured)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int CompletedEndCapCount
        {
            get
            {
                return CompletedFillingStripCount;
            }
        }

        public int CompletedFillingStripCount
        {
            get
            {
                return completedKimbapObject == null
                    ? 0
                    : CountCompletedFillingStrips(completedKimbapObject.transform);
            }
        }

        public Bounds RollBoundsForTests => rollRenderer == null ? default : rollRenderer.bounds;

        public int RollSortingOrderForTests => rollRenderer == null ? int.MinValue : rollRenderer.sortingOrder;

        public void Configure(KitchenController controller)
        {
            this.controller = controller;
        }

        public void ConfigurePrefabsForTests(GameObject rollGuidePrefab, GameObject completedKimbapPrefab, GameObject completedFillingCapPrefab)
        {
            this.rollGuidePrefab = rollGuidePrefab;
            this.completedKimbapPrefab = completedKimbapPrefab;
            this.completedFillingCapPrefab = completedFillingCapPrefab;
        }

        private void OnEnable()
        {
            if (rollButton != null)
            {
                rollButton.onClick.AddListener(HandleRollClicked);
            }

            if (completeButton != null)
            {
                completeButton.onClick.AddListener(HandleCompleteClicked);
            }
        }

        private void OnDisable()
        {
            if (rollButton != null)
            {
                rollButton.onClick.RemoveListener(HandleRollClicked);
            }

            if (completeButton != null)
            {
                completeButton.onClick.RemoveListener(HandleCompleteClicked);
            }
        }

        public void PlayRoll(Action onFinished)
        {
            if (isRolling)
            {
                return;
            }

            if (!PrepareRollVisual())
            {
                onFinished?.Invoke();
                return;
            }

            StartCoroutine(RollRoutine(onFinished));
        }

        public void FinalizeRoll()
        {
            DestroyRollVisual();
            HideFlatSeaweed();
            HideRiceSurface();
            HideOriginalFillings();
            CreateCompletedKimbap();
        }

        private void HandleRollClicked()
        {
            if (rollButton != null)
            {
                rollButton.interactable = false;
            }

            PlayRoll(() =>
            {
                if (rollButton != null)
                {
                    rollButton.gameObject.SetActive(false);
                }

                if (completeButton != null)
                {
                    completeButton.gameObject.SetActive(true);
                }
            });
        }

        private void HandleCompleteClicked()
        {
            FinalizeRoll();
            if (controller != null)
            {
                controller.CompleteAndSave();
            }

            if (completeButton != null)
            {
                completeButton.gameObject.SetActive(false);
            }

            if (submitButton != null)
            {
                submitButton.gameObject.SetActive(true);
            }
        }

        public void PrepareRollForTests()
        {
            PrepareRollVisual();
        }

        public void SimulateRollStepForTests(float moveProgress)
        {
            if (!hasPreparedRoll && !PrepareRollVisual())
            {
                return;
            }

            float rollHeight = originalSeaweedBounds.size.y / 3f;
            SetRollRect(rollHeight, Mathf.Clamp01(moveProgress));
            CaptureOverlappingFillings();
            MoveCapturedFillings();
        }

        private IEnumerator RollRoutine(Action onFinished)
        {
            isRolling = true;

            float rollHeight = originalSeaweedBounds.size.y / 3f;
            float elapsed = 0f;
            while (elapsed < growDuration)
            {
                elapsed += Time.deltaTime;
                float t = growDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / growDuration);
                SetRollRect(rollHeight * t, 0f);
                CaptureOverlappingFillings();
                MoveCapturedFillings();
                yield return null;
            }

            SetRollRect(rollHeight, 0f);
            CaptureOverlappingFillings();
            MoveCapturedFillings();

            elapsed = 0f;
            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = moveDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / moveDuration);
                SetRollRect(rollHeight, t);
                CaptureOverlappingFillings();
                MoveCapturedFillings();
                ApplySeaweedRemaining(1f - t);
                yield return null;
            }

            SetRollRect(rollHeight, 1f);
            CaptureOverlappingFillings();
            MoveCapturedFillings();
            ApplySeaweedRemaining(0f);
            HideFlatSeaweed();
            isRolling = false;
            onFinished?.Invoke();
        }

        private bool PrepareRollVisual()
        {
            if (controller == null || controller.TopSeaweedObject == null)
            {
                return false;
            }

            seaweedTransform = controller.TopSeaweedObject.transform;
            seaweedRenderer = controller.TopSeaweedObject.GetComponent<SpriteRenderer>();
            if (seaweedRenderer == null)
            {
                seaweedRenderer = controller.TopSeaweedObject.GetComponentInChildren<SpriteRenderer>();
            }

            if (seaweedRenderer == null)
            {
                return false;
            }

            DestroyRollVisual();
            DestroyCompletedKimbap();
            fillingStates.Clear();
            capturedFillingSlotCount = 0;

            originalSeaweedScale = seaweedTransform.localScale;
            originalSeaweedPosition = seaweedTransform.position;
            originalSeaweedBounds = seaweedRenderer.bounds;
            visualParent = seaweedTransform.parent;
            hasPreparedRoll = true;
            CacheFillings();

            if (rollGuidePrefab == null)
            {
                Debug.LogWarning("KitchenRollAnimator requires a roll guide prefab.");
                return false;
            }

            rollObject = Instantiate(rollGuidePrefab, visualParent, true);
            rollObject.name = "RollGuideRect";
            rollObject.transform.localScale = new Vector3(originalSeaweedBounds.size.x, 0f, 1f);

            rollRenderer = rollObject.GetComponent<SpriteRenderer>();
            if (rollRenderer == null)
            {
                Debug.LogWarning("Roll guide prefab must contain a SpriteRenderer.");
                DestroyRollVisual();
                return false;
            }

            if (rollRenderer.sprite == null)
            {
                rollRenderer.sprite = KitchenPlaceholderFactory.CreateWhiteSprite();
            }

            rollRenderer.color = rollColor;
            rollRenderer.sortingOrder = RollSortingOrder;
            SetRollRect(0f, 0f);
            return true;
        }

        private void CacheFillings()
        {
            if (controller == null)
            {
                return;
            }

            IReadOnlyList<GameObject> fillings = controller.DroppedFillingObjects;
            for (int i = 0; i < fillings.Count; i++)
            {
                GameObject fillingObject = fillings[i];
                if (fillingObject == null)
                {
                    continue;
                }

                SpriteRenderer renderer = fillingObject.GetComponent<SpriteRenderer>();
                if (renderer == null)
                {
                    renderer = fillingObject.GetComponentInChildren<SpriteRenderer>();
                }

                if (renderer == null)
                {
                    continue;
                }

                fillingStates.Add(new FillingRollState(fillingObject.transform, renderer, renderer.color));
            }
        }

        private void SetRollRect(float height, float moveProgress)
        {
            if (rollObject == null)
            {
                return;
            }

            float rollHeight = Mathf.Max(0f, height);
            float bottomCenterY = originalSeaweedBounds.min.y + (rollHeight * 0.5f);
            float topCenterY = originalSeaweedBounds.max.y - (rollHeight * 0.5f);
            float centerY = Mathf.Lerp(bottomCenterY, topCenterY, Mathf.Clamp01(moveProgress));
            rollObject.transform.position = new Vector3(
                originalSeaweedBounds.center.x,
                centerY,
                originalSeaweedPosition.z - 0.2f);
            rollObject.transform.localScale = new Vector3(
                originalSeaweedBounds.size.x,
                rollHeight,
                1f);
        }

        private void CaptureOverlappingFillings()
        {
            if (rollRenderer == null)
            {
                return;
            }

            Bounds rollBounds = rollRenderer.bounds;
            for (int i = 0; i < fillingStates.Count; i++)
            {
                FillingRollState state = fillingStates[i];
                if (state.IsCaptured || state.Renderer == null || !state.Renderer.enabled)
                {
                    continue;
                }

                if (!rollBounds.Intersects(state.Renderer.bounds))
                {
                    continue;
                }

                state.Capture(capturedFillingSlotCount);
                capturedFillingSlotCount++;
                ApplyFillingSortingOrder(state.Transform.gameObject, CapturedFillingSortingBase + i);
            }
        }

        private void MoveCapturedFillings()
        {
            if (rollObject == null || rollRenderer == null || capturedFillingSlotCount <= 0)
            {
                return;
            }

            Bounds rollBounds = rollRenderer.bounds;
            float usableHeight = Mathf.Max(0.01f, rollBounds.size.y * 0.3f);
            float startY = capturedFillingSlotCount == 1 ? 0f : -usableHeight * 0.5f;
            float stepY = capturedFillingSlotCount == 1 ? 0f : usableHeight / (capturedFillingSlotCount - 1);

            for (int i = 0; i < fillingStates.Count; i++)
            {
                FillingRollState state = fillingStates[i];
                if (!state.IsCaptured || state.Renderer == null)
                {
                    continue;
                }

                float yOffset = startY + (stepY * state.SlotIndex);
                state.MoveInsideRoll(new Vector3(
                    rollBounds.center.x,
                    rollBounds.center.y + yOffset,
                    rollObject.transform.position.z - 0.05f));
            }
        }

        private void ApplySeaweedRemaining(float remainingFraction)
        {
            if (seaweedTransform == null)
            {
                return;
            }

            float clampedRemaining = Mathf.Clamp01(remainingFraction);
            Vector3 scale = originalSeaweedScale;
            scale.y = originalSeaweedScale.y * Mathf.Max(clampedRemaining, MinimumVisibleScale);
            seaweedTransform.localScale = scale;

            float removedHeight = originalSeaweedBounds.size.y * (1f - clampedRemaining);
            seaweedTransform.position = originalSeaweedPosition + Vector3.up * (removedHeight * 0.5f);
        }

        private void HideFlatSeaweed()
        {
            if (seaweedRenderer != null)
            {
                seaweedRenderer.enabled = false;
            }
        }

        private void HideRiceSurface()
        {
            if (controller == null || controller.CurrentRiceSurface == null)
            {
                return;
            }

            controller.CurrentRiceSurface.gameObject.SetActive(false);
        }

        private void HideOriginalFillings()
        {
            for (int i = 0; i < fillingStates.Count; i++)
            {
                fillingStates[i].HideRenderer();
            }
        }

        private void CreateCompletedKimbap()
        {
            if (completedKimbapObject != null)
            {
                return;
            }

            Bounds bounds = hasPreparedRoll ? originalSeaweedBounds : ResolveCurrentBounds();
            Transform parent = visualParent != null ? visualParent : transform;
            if (completedKimbapPrefab == null)
            {
                Debug.LogWarning("KitchenRollAnimator requires a completed kimbap prefab.");
                return;
            }

            completedKimbapObject = Instantiate(completedKimbapPrefab, parent, true);
            completedKimbapObject.name = "CompletedKimbapPreview";
            completedKimbapObject.transform.position = new Vector3(bounds.center.x, bounds.center.y, originalSeaweedPosition.z - 0.25f);
            completedKimbapObject.transform.localPosition += completedPreviewLocalOffset;
            completedKimbapObject.transform.localScale = Vector3.one;
            DisableRenderer(completedKimbapObject.GetComponent<SpriteRenderer>());

            Vector2 bodySize = new Vector2(
                bounds.size.x * Mathf.Max(0.01f, completedBodySizeRatio.x),
                bounds.size.y * Mathf.Max(0.01f, completedBodySizeRatio.y));
            CreateFillingStrips(bodySize);
            CreateRectChild(
                "CompletedKimbapBody",
                Vector3.zero,
                bodySize,
                completedOuterColor,
                BodySortingOrder);
        }

        private void CreateFillingStrips(Vector2 bodySize)
        {
            if (fillingStates.Count == 0 || completedFillingCapPrefab == null)
            {
                return;
            }

            float verticalInset = Mathf.Clamp(completedFillingVerticalInset, 0f, bodySize.y * 0.45f);
            float innerHeight = Mathf.Max(0.01f, bodySize.y - (verticalInset * 2f));
            float stripHeight = Mathf.Max(
                0.01f,
                innerHeight / Mathf.Max(1, fillingStates.Count) * Mathf.Max(0.01f, completedFillingHeightScale));
            float usableHeight = Mathf.Max(0.01f, innerHeight - stripHeight);
            float startY = fillingStates.Count == 1 ? 0f : -usableHeight * 0.5f;
            float stepY = fillingStates.Count == 1 ? 0f : usableHeight / (fillingStates.Count - 1);
            float widthOffset = Mathf.Max(0f, completedFillingWidthOffset);
            Vector2 stripSize = new Vector2(bodySize.x + (widthOffset * 2f), stripHeight);

            for (int i = 0; i < fillingStates.Count; i++)
            {
                float y = startY + (stepY * i);
                CreateFillingStrip(
                    $"CompletedFillingStrip_{i}",
                    new Vector3(0f, y, 0.03f),
                    stripSize,
                    fillingStates[i].Color,
                    FillingStripSortingOrder + i);
            }
        }

        private void CreateFillingStrip(string objectName, Vector3 localPosition, Vector2 size, Color color, int sortingOrder)
        {
            GameObject stripObject = Instantiate(completedFillingCapPrefab, completedKimbapObject.transform, false);
            stripObject.name = objectName;
            stripObject.transform.localPosition = localPosition;
            stripObject.transform.localScale = new Vector3(size.x, size.y, 1f);

            SpriteRenderer renderer = stripObject.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                return;
            }

            if (renderer.sprite == null)
            {
                renderer.sprite = KitchenPlaceholderFactory.CreateWhiteSprite();
            }

            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
        }

        private GameObject CreateRectChild(string objectName, Vector3 localPosition, Vector2 size, Color color, int sortingOrder)
        {
            GameObject rectObject = new GameObject(objectName);
            rectObject.transform.SetParent(completedKimbapObject.transform, false);
            rectObject.transform.localPosition = localPosition;
            rectObject.transform.localScale = new Vector3(size.x, size.y, 1f);

            SpriteRenderer renderer = rectObject.AddComponent<SpriteRenderer>();
            renderer.sprite = KitchenPlaceholderFactory.CreateWhiteSprite();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return rectObject;
        }

        private Bounds ResolveCurrentBounds()
        {
            if (controller != null && controller.TopSeaweedObject != null)
            {
                SpriteRenderer renderer = controller.TopSeaweedObject.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    return renderer.bounds;
                }
            }

            return new Bounds(transform.position, new Vector3(2.4f, 1.7f, 1f));
        }

        private void DestroyRollVisual()
        {
            if (rollObject != null)
            {
                DestroyUnityObject(rollObject);
                rollObject = null;
                rollRenderer = null;
            }
        }

        private void DestroyCompletedKimbap()
        {
            if (completedKimbapObject != null)
            {
                DestroyUnityObject(completedKimbapObject);
                completedKimbapObject = null;
            }
        }

        private static void ApplyFillingSortingOrder(GameObject target, int sortingOrder)
        {
            SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sortingOrder = sortingOrder + i;
            }
        }

        private static int CountCompletedFillingStrips(Transform root)
        {
            int count = 0;
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                string objectName = transforms[i].name;
                if (objectName.StartsWith("CompletedFillingStrip_", StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static void DisableRenderer(SpriteRenderer renderer)
        {
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }

        private static void DestroyUnityObject(UnityEngine.Object target)
        {
            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private sealed class FillingRollState
        {
            public FillingRollState(Transform transform, SpriteRenderer renderer, Color color)
            {
                Transform = transform;
                Renderer = renderer;
                Color = color;
            }

            public Transform Transform { get; }

            public SpriteRenderer Renderer { get; }

            public Color Color { get; }

            public bool IsCaptured { get; private set; }

            public int SlotIndex { get; private set; }

            public void Capture(int slotIndex)
            {
                IsCaptured = true;
                SlotIndex = slotIndex;
            }

            public void MoveInsideRoll(Vector3 position)
            {
                if (!IsCaptured || Transform == null)
                {
                    return;
                }

                Transform.position = position;
            }

            public void HideRenderer()
            {
                if (Renderer != null)
                {
                    Renderer.enabled = false;
                }
            }
        }
    }
}
