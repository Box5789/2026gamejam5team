using System;
using GameJam.Gameplay.Spreading;
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
        [SerializeField] private Sprite visualSprite;
        [SerializeField] private Sprite dragPreviewSprite;
        [SerializeField] private GameObject dragPrefab;
        [SerializeField] private string riceBrushId;
        [SerializeField] private SpreadBrushDefinition riceBrushDefinition;

        public string VariantId => string.IsNullOrWhiteSpace(variantId) ? ingredientType.ToString() : variantId;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? VariantId : displayName;

        public IngredientType IngredientType => ingredientType;

        public KitchenIngredientCategory Category => category;

        public Color PlaceholderColor => placeholderColor;

        public Sprite VisualSprite => visualSprite;

        public Sprite DragPreviewSprite => dragPreviewSprite;

        public GameObject DragPrefab => dragPrefab;

        public string RiceBrushId
        {
            get
            {
                if (riceBrushDefinition != null && !string.IsNullOrWhiteSpace(riceBrushDefinition.Id))
                {
                    return riceBrushDefinition.Id;
                }

                return string.IsNullOrWhiteSpace(riceBrushId) ? VariantId : riceBrushId;
            }
        }

        public SpreadBrushDefinition RiceBrushDefinition => riceBrushDefinition;

        public KitchenIngredientDefinition(
            string variantId,
            string displayName,
            IngredientType ingredientType,
            KitchenIngredientCategory category,
            Color placeholderColor,
            string riceBrushId = "")
            : this(variantId, displayName, ingredientType, category, placeholderColor, null, riceBrushId)
        {
        }

        public KitchenIngredientDefinition(
            string variantId,
            string displayName,
            IngredientType ingredientType,
            KitchenIngredientCategory category,
            Color placeholderColor,
            GameObject dragPrefab,
            string riceBrushId = "")
            : this(variantId, displayName, ingredientType, category, placeholderColor, dragPrefab, riceBrushId, null)
        {
        }

        public KitchenIngredientDefinition(
            string variantId,
            string displayName,
            IngredientType ingredientType,
            KitchenIngredientCategory category,
            Color placeholderColor,
            GameObject dragPrefab,
            string riceBrushId,
            SpreadBrushDefinition riceBrushDefinition)
            : this(variantId, displayName, ingredientType, category, placeholderColor, dragPrefab, null, riceBrushId, riceBrushDefinition)
        {
        }

        public KitchenIngredientDefinition(
            string variantId,
            string displayName,
            IngredientType ingredientType,
            KitchenIngredientCategory category,
            Color placeholderColor,
            GameObject dragPrefab,
            Sprite visualSprite,
            string riceBrushId,
            SpreadBrushDefinition riceBrushDefinition)
            : this(
                variantId,
                displayName,
                ingredientType,
                category,
                placeholderColor,
                dragPrefab,
                visualSprite,
                null,
                riceBrushId,
                riceBrushDefinition)
        {
        }

        public KitchenIngredientDefinition(
            string variantId,
            string displayName,
            IngredientType ingredientType,
            KitchenIngredientCategory category,
            Color placeholderColor,
            GameObject dragPrefab,
            Sprite visualSprite,
            Sprite dragPreviewSprite,
            string riceBrushId,
            SpreadBrushDefinition riceBrushDefinition)
        {
            this.variantId = variantId;
            this.displayName = displayName;
            this.ingredientType = ingredientType;
            this.category = category;
            this.placeholderColor = placeholderColor;
            this.visualSprite = visualSprite;
            this.dragPreviewSprite = dragPreviewSprite;
            this.dragPrefab = dragPrefab;
            this.riceBrushId = riceBrushId;
            this.riceBrushDefinition = riceBrushDefinition;
        }
    }
}
