using System;
using KimbapGame.Data;
using UnityEngine;

namespace KimbapGame.Kitchen
{
    [Serializable]
    public class KitchenIngredientDefinition
    {
        [SerializeField] private string variantId;
        [SerializeField] private string displayName;
        [SerializeField] private IngredientType ingredientType;
        [SerializeField] private KitchenIngredientCategory category;
        [SerializeField] private Color placeholderColor = Color.white;
        [SerializeField] private GameObject dragPrefab;
        [SerializeField] private string riceBrushId;

        public string VariantId => string.IsNullOrWhiteSpace(variantId) ? ingredientType.ToString() : variantId;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? VariantId : displayName;

        public IngredientType IngredientType => ingredientType;

        public KitchenIngredientCategory Category => category;

        public Color PlaceholderColor => placeholderColor;

        public GameObject DragPrefab => dragPrefab;

        public string RiceBrushId => string.IsNullOrWhiteSpace(riceBrushId) ? VariantId : riceBrushId;

        public KitchenIngredientDefinition(
            string variantId,
            string displayName,
            IngredientType ingredientType,
            KitchenIngredientCategory category,
            Color placeholderColor,
            string riceBrushId = "")
        {
            this.variantId = variantId;
            this.displayName = displayName;
            this.ingredientType = ingredientType;
            this.category = category;
            this.placeholderColor = placeholderColor;
            this.riceBrushId = riceBrushId;
        }
    }
}
