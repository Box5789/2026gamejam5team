using System;
using System.Collections.Generic;
using System.Globalization;
using KimbapGame.Data;

namespace KimbapGame.Kitchen
{
    public sealed class KitchenIngredientCatalog
    {
        private readonly List<KitchenIngredientCatalogItem> items;

        private KitchenIngredientCatalog(List<KitchenIngredientCatalogItem> items)
        {
            this.items = items;
        }

        public IReadOnlyList<KitchenIngredientCatalogItem> Items => items;

        public IEnumerable<KitchenIngredientCatalogItem> SourceItems
        {
            get
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].IsSourceCategory)
                    {
                        yield return items[i];
                    }
                }
            }
        }

        public static KitchenIngredientCatalog Parse(string csv)
        {
            List<List<string>> rows = CsvTableParser.Parse(csv);
            List<KitchenIngredientCatalogItem> items = new List<KitchenIngredientCatalogItem>();
            if (rows.Count <= 1)
            {
                return new KitchenIngredientCatalog(items);
            }

            Dictionary<string, int> header = CsvTableParser.BuildHeader(rows[0]);
            for (int i = 1; i < rows.Count; i++)
            {
                List<string> row = rows[i];
                if (CsvTableParser.IsEmptyRow(row))
                {
                    continue;
                }

                string id = CsvTableParser.GetAny(row, header, "Index", "인덱스", "순서", "번호");
                string displayName = CsvTableParser.GetAny(row, header, "이름", "Name", "DisplayName");
                string categoryText = CsvTableParser.GetAny(row, header, "분류", "Category");
                if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(displayName) && string.IsNullOrWhiteSpace(categoryText))
                {
                    continue;
                }

                items.Add(new KitchenIngredientCatalogItem(
                    id,
                    displayName,
                    ParseCategory(categoryText),
                    ParsePrice(CsvTableParser.GetAny(row, header, "가격", "Price")),
                    CsvTableParser.GetAny(row, header, "이미지", "Image", "ImageName"),
                    ParseRequired(CsvTableParser.GetAny(row, header, "필수여부", "필수", "Required", "IsRequired")),
                    CsvTableParser.GetAny(row, header, "브러시ID", "브러쉬ID", "브러시아이디", "브러쉬아이디", "BrushId", "RiceBrushId"),
                    CsvTableParser.GetAny(row, header, "브러시이미지", "브러쉬이미지", "브러시패턴", "브러쉬패턴", "BrushImage", "BrushTexture", "BrushTexturePath", "Pattern"),
                    ParseNullableFloat(CsvTableParser.GetAny(row, header, "브러시반경", "브러쉬반경", "반경", "BrushRadius", "Radius")),
                    ParseNullableInt(CsvTableParser.GetAny(row, header, "입자수", "ParticlesPerSample", "Particles")),
                    ParseNullableFloat(CsvTableParser.GetAny(row, header, "흩뿌림반경", "퍼짐반경", "ScatterRadius", "Scatter")),
                    ParseNullableFloat(CsvTableParser.GetAny(row, header, "최소크기", "MinScale", "MinimumScale")),
                    ParseNullableFloat(CsvTableParser.GetAny(row, header, "최대크기", "MaxScale", "MaximumScale")),
                    ParseNullableFloat(CsvTableParser.GetAny(row, header, "스탬프간격", "간격", "StampSpacing", "Spacing"))));
            }

            return new KitchenIngredientCatalog(items);
        }

        public static KitchenIngredientCategory ParseCategory(string value)
        {
            string normalized = CsvTableParser.Normalize(value);
            switch (normalized)
            {
                case "김":
                case "seaweed":
                    return KitchenIngredientCategory.Seaweed;
                case "밥":
                case "rice":
                    return KitchenIngredientCategory.Rice;
                case "속":
                case "속재료":
                case "filling":
                    return KitchenIngredientCategory.Filling;
                case "소스":
                case "sauce":
                    return KitchenIngredientCategory.Sauce;
                default:
                    throw new FormatException($"Unknown kitchen ingredient category '{value}'.");
            }
        }

        private static int ParsePrice(string value)
        {
            string normalized = (value ?? string.Empty)
                .Trim()
                .Replace(",", string.Empty)
                .Replace("원", string.Empty);
            return int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out int price)
                ? Math.Max(0, price)
                : 0;
        }

        private static bool ParseRequired(string value)
        {
            string normalized = CsvTableParser.Normalize(value);
            return normalized == "1"
                || normalized == "true"
                || normalized == "yes"
                || normalized == "y"
                || normalized == "o"
                || normalized == "필수"
                || normalized == "required";
        }

        private static float? ParseNullableFloat(string value)
        {
            string normalized = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return null;
            }

            normalized = normalized.Replace("f", string.Empty).Replace("F", string.Empty);
            return float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out float result)
                ? result
                : null;
        }

        private static int? ParseNullableInt(string value)
        {
            string normalized = (value ?? string.Empty).Trim();
            return int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result)
                ? result
                : null;
        }
    }
}
