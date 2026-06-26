using System;
using System.Collections.Generic;
using KimbapGame.Data;

namespace KimbapGame.Gameplay
{
    [Serializable]
    public class RecipeCheckResult
    {
        public bool isSuccess;
        public List<IngredientType> expectedIngredients = new List<IngredientType>();
        public List<IngredientType> actualIngredients = new List<IngredientType>();
        public string message;

        public RecipeCheckResult(bool isSuccess, List<IngredientType> expectedIngredients, List<IngredientType> actualIngredients, string message)
        {
            this.isSuccess = isSuccess;
            this.expectedIngredients = expectedIngredients;
            this.actualIngredients = actualIngredients;
            this.message = message;
        }
    }
}
