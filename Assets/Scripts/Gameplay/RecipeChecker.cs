using System.Collections.Generic;
using KimbapGame.Data;

namespace KimbapGame.Gameplay
{
    public static class RecipeChecker
    {
        public static RecipeCheckResult Check(OrderData orderData, PlayerKimbap playerKimbap)
        {
            List<IngredientType> expected = orderData != null
                ? new List<IngredientType>(orderData.ingredients)
                : new List<IngredientType>();
            List<IngredientType> actual = playerKimbap != null
                ? new List<IngredientType>(playerKimbap.ingredients)
                : new List<IngredientType>();

            if (orderData == null)
            {
                return new RecipeCheckResult(false, expected, actual, "Order data is missing.");
            }

            if (playerKimbap == null)
            {
                return new RecipeCheckResult(false, expected, actual, "Player kimbap data is missing.");
            }

            bool isSuccess = IsSameOrder(expected, actual);
            string message = isSuccess ? "Recipe matched." : "Recipe did not match.";
            return new RecipeCheckResult(isSuccess, expected, actual, message);
        }

        private static bool IsSameOrder(IReadOnlyList<IngredientType> expected, IReadOnlyList<IngredientType> actual)
        {
            if (expected.Count != actual.Count)
            {
                return false;
            }

            for (int i = 0; i < expected.Count; i++)
            {
                if (expected[i] != actual[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
