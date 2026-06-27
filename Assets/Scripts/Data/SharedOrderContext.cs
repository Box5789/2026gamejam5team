using System;
using KimbapGame.Evaluation;

namespace KimbapGame.Data
{
    public static class SharedOrderContext
    {
        public static CurrentOrder CurrentOrder { get; private set; }
        public static SheetOrderData CurrentSheetOrder { get; private set; }
        public static KimbapEvaluationResult PendingEvaluationResult { get; private set; }
        public static string ReturnSceneName { get; set; } = "order";

        public static bool HasOrder => CurrentOrder != null && CurrentOrder.order != null;
        public static bool HasPendingEvaluation => PendingEvaluationResult != null;

        public static event Action<CurrentOrder> CurrentOrderChanged;

        public static void SetCurrentOrder(OrderData orderData, SheetOrderData sheetOrderData = null)
        {
            CurrentOrder = new CurrentOrder
            {
                order = orderData,
                isCompleted = false
            };
            CurrentSheetOrder = sheetOrderData;
            PendingEvaluationResult = null;
            CurrentOrderChanged?.Invoke(CurrentOrder);
        }

        public static void SetEvaluationResult(KimbapEvaluationResult result)
        {
            PendingEvaluationResult = result;
        }

        public static void ClearEvaluationResult()
        {
            PendingEvaluationResult = null;
        }

        public static void CompleteCurrentOrder()
        {
            if (CurrentOrder == null)
            {
                return;
            }

            CurrentOrder.isCompleted = true;
            CurrentOrderChanged?.Invoke(CurrentOrder);
        }

        public static void Clear()
        {
            CurrentOrder = null;
            CurrentSheetOrder = null;
            PendingEvaluationResult = null;
            CurrentOrderChanged?.Invoke(null);
        }
    }
}
