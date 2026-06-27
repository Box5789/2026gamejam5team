using KimbapGame.Data;

namespace KimbapGame.Order
{
    public static class IngredientNameMapper
    {
        public static IngredientType ToIngredientType(string value, IngredientType? fallback = null)
        {
            return IngredientTypeMapper.ToIngredientType(value, fallback ?? IngredientType.Ham);
        }
    }
}
