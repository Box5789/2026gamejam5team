using System;
using System.Collections.Generic;

namespace KimbapGame.Data
{
    [Serializable]
    public class OrderData
    {
        public int orderId;
        public string orderName;
        public string customerDialogue;
        public List<IngredientType> ingredients = new List<IngredientType>();
    }
}
