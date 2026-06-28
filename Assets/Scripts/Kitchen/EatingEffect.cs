using KimbapGame.Data;
using KimbapGame.Evaluation;
using KimbapGame.Kitchen;
using UnityEngine;

public class EatingEffect : MonoBehaviour
{
    [SerializeField] private string maskSortingLayerName = "MaskTarget";
    [SerializeField] private int coverSortingOrder = 190;

    private void OnEnable()
    {
        SharedOrderContext.EvaluationResultChanged += OnEvaluationResultChanged;

        if (SharedOrderContext.HasPendingEvaluation)
            OnEvaluationResultChanged(SharedOrderContext.PendingEvaluationResult);
    }

    private void OnDisable()
    {
        SharedOrderContext.EvaluationResultChanged -= OnEvaluationResultChanged;
    }

    private void OnEvaluationResultChanged(KimbapEvaluationResult result)
    {
        ApplyBiteMask();
    }

    private void ApplyBiteMask()
    {
        var renderers = GetComponentsInChildren<SpriteRenderer>(true);

        foreach (var sr in renderers)
        {
            sr.sortingLayerName = maskSortingLayerName;
            sr.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
        }

        foreach (var cover in GetComponentsInChildren<KitchenRollSeaweedCover>(true))
        {
            if (cover.CoverRenderer == null)
                continue;

            cover.CoverRenderer.sortingLayerName = maskSortingLayerName;
            cover.CoverRenderer.sortingOrder = coverSortingOrder;
            cover.CoverRenderer.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
        }
    }
}