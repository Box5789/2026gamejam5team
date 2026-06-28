using System;
using System.Collections.Generic;

namespace KimbapGame.Data
{
    [Serializable]
    public class SheetOrderData
    {
        public string index;
        public string customerName;
        public string orderDialogue;
        public string orderImageName;
        public string hintDialogue;
        public string hintImageName;
        public string successDialogue;
        public string successImageName;
        public string failDialogue;
        public string failImageName;
        public string matchedImageName;
        public string seaweedName;
        public int seaweedCount;
        public string riceName;
        public int riceCount;
        public string fillingName;
        public int fillingCount;
        public List<IngredientType> ingredients = new List<IngredientType>();

        public OrderData ToOrderData()
        {
            return new OrderData
            {
                orderId = ParseOrderId(index),
                orderName = string.IsNullOrWhiteSpace(customerName) ? index : customerName,
                customerDialogue = orderDialogue,
                ingredients = new List<IngredientType>(ingredients)
            };
        }

        private static int ParseOrderId(string value)
        {
            if (int.TryParse(value, out int number))
            {
                return number;
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            int result = 0;
            foreach (char c in value)
            {
                if (char.IsDigit(c))
                {
                    result = (result * 10) + (c - '0');
                }
            }

            return result;
        }
    }
}
