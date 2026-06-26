using KimbapGame.Data;

namespace KimbapGame.Order
{
    public static class IngredientNameMapper
    {
        public static IngredientType ToIngredientType(string value, IngredientType? fallback = null)
        {
            string normalized = Normalize(value);
            switch (normalized)
            {
                case "김":
                case "seaweed":
                    return IngredientType.Seaweed;
                case "밥":
                case "rice":
                    return IngredientType.Rice;
                case "햄":
                case "ham":
                    return IngredientType.Ham;
                case "계란":
                case "달걀":
                case "egg":
                    return IngredientType.Egg;
                case "당근":
                case "carrot":
                    return IngredientType.Carrot;
                case "시금치":
                case "spinach":
                    return IngredientType.Spinach;
                case "참치":
                case "tuna":
                    return IngredientType.Tuna;
                case "맛살":
                case "crabmeat":
                case "crab":
                    return IngredientType.CrabMeat;
                case "단무지":
                case "pickledradish":
                case "radish":
                    return IngredientType.PickledRadish;
                default:
                    return fallback ?? IngredientType.Ham;
            }
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Trim().Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
