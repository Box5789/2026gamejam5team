using System;
using GameJam.Gameplay.Spreading;
using KimbapGame.Data;
using UnityEngine;

namespace KimbapGame.Kitchen
{
    public sealed class KitchenIngredientCatalogItem
    {
        public KitchenIngredientCatalogItem(
            string id,
            string displayName,
            KitchenIngredientCategory category,
            int price,
            string imageName,
            bool isRequired)
            : this(
                id,
                displayName,
                category,
                price,
                imageName,
                isRequired,
                string.Empty,
                string.Empty,
                null,
                null,
                null,
                null,
                null,
                null)
        {
        }

        public KitchenIngredientCatalogItem(
            string id,
            string displayName,
            KitchenIngredientCategory category,
            int price,
            string imageName,
            bool isRequired,
            string brushId,
            string brushImagePath,
            float? brushRadius,
            int? particlesPerSample,
            float? scatterRadius,
            float? minScale,
            float? maxScale,
            float? stampSpacing)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Category = category;
            Price = price;
            ImageName = imageName ?? string.Empty;
            IsRequired = isRequired;
            BrushId = brushId ?? string.Empty;
            BrushImagePath = brushImagePath ?? string.Empty;
            BrushRadius = brushRadius;
            ParticlesPerSample = particlesPerSample;
            ScatterRadius = scatterRadius;
            MinScale = minScale;
            MaxScale = maxScale;
            StampSpacing = stampSpacing;
        }

        public string Id { get; }

        public string DisplayName { get; }

        public KitchenIngredientCategory Category { get; }

        public int Price { get; }

        public string ImageName { get; }

        public bool IsRequired { get; }

        public string BrushId { get; }

        public string BrushImagePath { get; }

        public float? BrushRadius { get; }

        public int? ParticlesPerSample { get; }

        public float? ScatterRadius { get; }

        public float? MinScale { get; }

        public float? MaxScale { get; }

        public float? StampSpacing { get; }

        public bool IsSourceCategory =>
            Category == KitchenIngredientCategory.Seaweed
            || Category == KitchenIngredientCategory.Rice
            || Category == KitchenIngredientCategory.Filling;

        public KitchenIngredientDefinition ToDefinition(GameObject dragPrefab, Color placeholderColor)
        {
            return ToDefinition(dragPrefab, placeholderColor, KitchenRiceBrushTextureLoader.Load);
        }

        public KitchenIngredientDefinition ToDefinition(
            GameObject dragPrefab,
            Color placeholderColor,
            Func<string, Texture2D> brushTextureResolver)
        {
            return ToDefinition(dragPrefab, placeholderColor, brushTextureResolver, KitchenIngredientSpriteLoader.Load);
        }

        public KitchenIngredientDefinition ToDefinition(
            GameObject dragPrefab,
            Color placeholderColor,
            Func<string, Texture2D> brushTextureResolver,
            Func<string, string, string, Sprite> visualSpriteResolver)
        {
            IngredientType ingredientType = IngredientTypeMapper.ToIngredientType(DisplayName, GetFallbackIngredientType());
            SpreadBrushDefinition riceBrushDefinition = CreateRiceBrushDefinition(placeholderColor, brushTextureResolver);
            Sprite visualSprite = visualSpriteResolver == null ? null : visualSpriteResolver(ImageName, DisplayName, Id);
            Sprite dragPreviewSprite = CreateDragPreviewSprite(visualSpriteResolver);
            return new KitchenIngredientDefinition(
                Id,
                DisplayName,
                ingredientType,
                Category,
                placeholderColor,
                dragPrefab,
                visualSprite,
                dragPreviewSprite,
                riceBrushDefinition == null ? string.Empty : riceBrushDefinition.Id,
                riceBrushDefinition);
        }

        private IngredientType GetFallbackIngredientType()
        {
            switch (Category)
            {
                case KitchenIngredientCategory.Seaweed:
                    return IngredientType.Seaweed;
                case KitchenIngredientCategory.Rice:
                    return IngredientType.Rice;
                default:
                    return IngredientType.GenericFilling;
            }
        }

        private SpreadBrushDefinition CreateRiceBrushDefinition(Color fallbackTint, Func<string, Texture2D> brushTextureResolver)
        {
            if (Category != KitchenIngredientCategory.Rice)
            {
                return null;
            }

            string id = GetRiceBrushId();
            Texture2D brushTexture = !string.IsNullOrWhiteSpace(BrushImagePath) && brushTextureResolver != null
                ? brushTextureResolver(BrushImagePath)
                : null;
            Color tint = brushTexture == null ? fallbackTint : Color.white;
            return new SpreadBrushDefinition(
                id,
                DisplayName,
                brushTexture,
                tint,
                BrushRadius ?? SpreadBrushDefinition.DefaultBrushRadius,
                ParticlesPerSample ?? SpreadBrushDefinition.DefaultParticlesPerSample,
                ScatterRadius ?? SpreadBrushDefinition.DefaultScatterRadius,
                MinScale ?? SpreadBrushDefinition.DefaultMinScale,
                MaxScale ?? SpreadBrushDefinition.DefaultMaxScale,
                StampSpacing ?? SpreadBrushDefinition.DefaultStampSpacing);
        }

        private Sprite CreateDragPreviewSprite(Func<string, string, string, Sprite> visualSpriteResolver)
        {
            if (Category != KitchenIngredientCategory.Filling
                || visualSpriteResolver == null
                || string.IsNullOrWhiteSpace(BrushImagePath))
            {
                return null;
            }

            return visualSpriteResolver(BrushImagePath, DisplayName, Id);
        }

        private string GetRiceBrushId()
        {
            if (!string.IsNullOrWhiteSpace(BrushId))
            {
                return BrushId;
            }

            if (!string.IsNullOrWhiteSpace(Id))
            {
                return Id;
            }

            return string.IsNullOrWhiteSpace(DisplayName) ? "rice" : DisplayName;
        }
    }
}
