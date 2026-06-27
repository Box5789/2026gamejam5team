namespace KimbapGame.Data
{
    public static class IngredientTypeMapper
    {
        public static IngredientType ToIngredientType(string value, IngredientType fallback)
        {
            string normalized = Normalize(value);
            switch (normalized)
            {
                case "김":
                case "기본김":
                case "김밥김":
                case "참기름김":
                case "구운김":
                case "적신김":
                case "소금김":
                case "매운김":
                case "간장김":
                case "마른김":
                case "두꺼운김":
                case "얇은김":
                case "검은김":
                case "파래김":
                case "김가루":
                case "탄김":
                case "seaweed":
                    return IngredientType.Seaweed;
                case "밥":
                case "쌀밥":
                case "흰밥":
                case "흰쌀밥":
                case "백미":
                case "현미":
                case "현미밥":
                case "흑미":
                case "흑미밥":
                case "보리밥":
                case "잡곡밥":
                case "찹쌀밥":
                case "콩밥":
                case "귀리밥":
                case "옥수수밥":
                case "녹차밥":
                case "rice":
                    return IngredientType.Rice;
                case "햄":
                case "ham":
                    return IngredientType.Ham;
                case "계란":
                case "달걀":
                case "계란지단":
                case "달걀말이":
                case "스크램블에그":
                case "egg":
                    return IngredientType.Egg;
                case "당근":
                case "carrot":
                    return IngredientType.Carrot;
                case "시금치":
                case "spinach":
                    return IngredientType.Spinach;
                case "참치":
                case "참치마요":
                case "tuna":
                    return IngredientType.Tuna;
                case "맛살":
                case "게맛살":
                case "크래미":
                case "crabmeat":
                case "crab":
                    return IngredientType.CrabMeat;
                case "단무지":
                case "pickledradish":
                case "radish":
                    return IngredientType.PickledRadish;
                default:
                    return fallback;
            }
        }

        public static string Normalize(string value)
        {
            return CsvTableParser.Normalize(value);
        }
    }
}
