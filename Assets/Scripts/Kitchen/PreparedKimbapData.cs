using System;
using System.Collections.Generic;
using System.Linq;
using KimbapGame.Data;

namespace KimbapGame.Kitchen
{
    [Serializable]
    public class PreparedKimbapItem
    {
        public IngredientType ingredientType;
        public KitchenIngredientCategory category;
        public string variantId;
        public string displayName;

        public PreparedKimbapItem(IngredientType ingredientType, KitchenIngredientCategory category, string variantId, string displayName)
        {
            this.ingredientType = ingredientType;
            this.category = category;
            this.variantId = variantId;
            this.displayName = displayName;
        }

        public string ToExportString()
        {
            return $"{ingredientType}:{variantId}:{displayName}";
        }
    }

    [Serializable]
    public class PreparedKimbapData
    {
        public readonly List<PreparedKimbapItem> seaweeds = new List<PreparedKimbapItem>();
        public readonly List<PreparedKimbapItem> riceItems = new List<PreparedKimbapItem>();
        public readonly List<PreparedKimbapItem> fillings = new List<PreparedKimbapItem>();

        public IEnumerable<PreparedKimbapItem> AllItems => seaweeds.Concat(riceItems).Concat(fillings);

        public void Clear()
        {
            seaweeds.Clear();
            riceItems.Clear();
            fillings.Clear();
        }
    }
}
