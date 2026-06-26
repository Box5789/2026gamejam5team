using System;
using System.Collections.Generic;

namespace KimbapGame.Data
{
    [Serializable]
    public class PlayerKimbap
    {
        public List<IngredientType> ingredients = new List<IngredientType>();

        public void AddIngredient(IngredientType ingredient)
        {
            ingredients.Add(ingredient);
        }

        public void Clear()
        {
            ingredients.Clear();
        }
    }
}
