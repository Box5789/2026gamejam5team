using System;
using System.Collections;
using UnityEngine;

namespace KimbapGame.Kitchen
{
    [DisallowMultipleComponent]
    public sealed class KitchenRollAnimator : MonoBehaviour
    {
        private const int RollSortingOrder = 160;
        private const float MinimumVisibleScale = 0.001f;

        [SerializeField] private KitchenController controller;
        [SerializeField] private float growDuration = 0.45f;
        [SerializeField] private float moveDuration = 0.8f;
        [SerializeField] private Color rollColor = new Color(1f, 0.78f, 0.18f, 1f);
        [SerializeField] private Color completedOuterColor = new Color(0.08f, 0.14f, 0.09f, 1f);
        [SerializeField] private Color completedInnerColor = new Color(1f, 0.93f, 0.66f, 1f);

        private GameObject rollObject;
        private GameObject completedKimbapObject;
        private Transform seaweedTransform;
        private SpriteRenderer seaweedRenderer;
        private Transform visualParent;
        private Vector3 originalSeaweedScale;
        private Vector3 originalSeaweedPosition;
        private Bounds originalSeaweedBounds;
        private bool isRolling;
        private bool hasPreparedRoll;

        public bool IsRolling => isRolling;

        public bool HasRollVisual => rollObject != null;

        public bool HasCompletedKimbap => completedKimbapObject != null;

        public void Configure(KitchenController controller)
        {
            this.controller = controller;
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
            CreateCompletedKimbap();
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
                yield return null;
            }

            SetRollRect(rollHeight, 0f);

            elapsed = 0f;
            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = moveDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / moveDuration);
                SetRollRect(rollHeight, t);
                ApplySeaweedRemaining(1f - t);
                yield return null;
            }

            SetRollRect(rollHeight, 1f);
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

            originalSeaweedScale = seaweedTransform.localScale;
            originalSeaweedPosition = seaweedTransform.position;
            originalSeaweedBounds = seaweedRenderer.bounds;
            visualParent = seaweedTransform.parent;
            hasPreparedRoll = true;

            rollObject = new GameObject("RollGuideRect");
            rollObject.transform.SetParent(visualParent, true);
            rollObject.transform.localScale = new Vector3(originalSeaweedBounds.size.x, 0f, 1f);

            SpriteRenderer rollRenderer = rollObject.AddComponent<SpriteRenderer>();
            rollRenderer.sprite = KitchenPlaceholderFactory.CreateWhiteSprite();
            rollRenderer.color = rollColor;
            rollRenderer.sortingOrder = RollSortingOrder;
            SetRollRect(0f, 0f);
            return true;
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

        private void CreateCompletedKimbap()
        {
            if (completedKimbapObject != null)
            {
                return;
            }

            Bounds bounds = hasPreparedRoll ? originalSeaweedBounds : ResolveCurrentBounds();
            Transform parent = visualParent != null ? visualParent : transform;
            completedKimbapObject = new GameObject("CompletedKimbapPreview");
            completedKimbapObject.transform.SetParent(parent, true);
            completedKimbapObject.transform.position = new Vector3(bounds.center.x, bounds.center.y, originalSeaweedPosition.z - 0.25f);

            KitchenPlaceholderFactory.CreateSpriteObject(
                "CompletedKimbapOuter",
                completedKimbapObject.transform,
                Vector3.zero,
                new Vector2(bounds.size.x * 0.92f, bounds.size.y * 0.36f),
                completedOuterColor,
                RollSortingOrder + 1);
            KitchenPlaceholderFactory.CreateSpriteObject(
                "CompletedKimbapInner",
                completedKimbapObject.transform,
                new Vector3(0f, 0f, -0.02f),
                new Vector2(bounds.size.x * 0.76f, bounds.size.y * 0.2f),
                completedInnerColor,
                RollSortingOrder + 2);
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
    }
}
