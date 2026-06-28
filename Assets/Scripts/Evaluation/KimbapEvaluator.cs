using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using KimbapGame.Data;
using KimbapGame.Kitchen;
using UnityEngine;

namespace KimbapGame.Evaluation
{
    public static class KimbapEvaluator
    {
        private const float SeaweedScore = 15f;
        private const float RiceCombinationScore = 10f;
        private const float RiceAmountScore = 10f;
        private const float FillingScore = 25f;
        private const float PriceScore = 15f;
        private const float SpecialScore = 5f;
        private const float AssemblyScore = 10f;
        private const float DetailScore = 10f;
        private const float CriticalFailureScoreCap = 39f;
        private const string IngredientsCsvRelativePath = "Kitchen/ingredients.csv";

        private static readonly Dictionary<string, string[]> TokenAliases = new Dictionary<string, string[]>
        {
            { "seaweed", new[] { "김", "기본김", "김밥김", "seaweed", "r1" } },
            { "sesame", new[] { "참기름", "참기름김", "sesame", "r2" } },
            { "roasted", new[] { "구운김", "구운", "굽", "roasted", "r3" } },
            { "wet", new[] { "적신김", "적신", "젖은", "wet", "r4" } },
            { "foil", new[] { "호일", "알루미늄호일", "알류미늄호일", "aluminumfoil", "aluminiumfoil", "foil", "r5" } },
            { "white", new[] { "흰쌀밥", "흰밥", "쌀밥", "백미", "white", "whiterice", "r16" } },
            { "brown", new[] { "현미밥", "현미", "brown", "brownrice", "r17" } },
            { "black", new[] { "흑미밥", "흑미", "black", "blackrice", "r18" } },
            { "ham", new[] { "햄", "ham", "r31" } },
            { "egg", new[] { "계란", "달걀", "계란지단", "달걀말이", "스크램블에그", "egg", "r30", "r91", "r92" } },
            { "carrot", new[] { "당근", "carrot", "r28" } },
            { "spinach", new[] { "시금치", "spinach", "r29" } },
            { "pickledradish", new[] { "단무지", "pickledradish", "radish", "r26" } },
            { "tuna", new[] { "참치", "참치마요", "tuna", "r35" } },
            { "crabmeat", new[] { "맛살", "게맛살", "크래미", "crabmeat", "crab", "r32", "r47" } },
            { "cucumber", new[] { "오이", "cucumber", "r33" } },
            { "stone", new[] { "돌", "돌멩이", "돌맹이", "stone", "rock", "r97" } }
        };

        private static List<CatalogIngredient> catalogIngredients;

        public static KimbapEvaluationResult Evaluate(SheetOrderData order, PreparedKimbapData preparedKimbap)
        {
            KimbapEvaluationResult result = new KimbapEvaluationResult();
            if (order == null)
            {
                result.AddCategory("Order data", KimbapEvaluationResult.BaseMaxScore, 0f, "No order data was available.");
                result.FinalizeScore(KimbapEvaluationResult.BaseMaxScore, string.Empty, string.Empty, "주문 정보가 없어 평가할 수 없어요.", string.Empty);
                return result;
            }

            PreparedKimbapData prepared = preparedKimbap ?? new PreparedKimbapData();
            EvaluationProfile profile = EvaluationProfile.FromOrder(order);

            result.AddCategory("Seaweed", SeaweedScore, ScoreSeaweed(profile, prepared), string.Empty);
            result.AddCategory("Rice combination", RiceCombinationScore, ScoreRiceCombination(profile, prepared), string.Empty);
            result.AddCategory("Rice amount", RiceAmountScore, ScoreRiceAmount(profile, prepared), string.Empty);
            result.AddCategory("Fillings", FillingScore, ScoreFillings(profile, prepared), string.Empty);
            result.AddCategory("Price", PriceScore, ScorePrice(profile, prepared), string.Empty);
            result.AddCategory("Special ingredients", SpecialScore, ScoreSpecialIngredients(profile, prepared), string.Empty);
            result.AddCategory("Assembly", AssemblyScore, ScoreAssembly(prepared), string.Empty);
            result.AddCategory("Details", DetailScore, ScoreDetails(profile, prepared), string.Empty);
            ApplyCriticalFailureCap(result, profile, prepared);
            result.FinalizeScore(profile.maxScore, order.successDialogue, order.successImageName, order.failDialogue, order.failImageName);
            return result;
        }

        public static KimbapEvaluationResult EvaluateCurrentOrder(PreparedKimbapData preparedKimbap)
        {
            return Evaluate(SharedOrderContext.CurrentSheetOrder, preparedKimbap);
        }

        public static KimbapEvaluationResult Evaluate(SheetOrderData order, KimbapResultRow resultRow)
        {
            return Evaluate(order, ToPreparedKimbapData(resultRow));
        }

        public static KimbapEvaluationResult EvaluateLatestResultRow(SheetOrderData order, string xlsxPath)
        {
            List<KimbapResultRow> rows = KimbapXlsxReader.Read(xlsxPath);
            return rows.Count == 0
                ? Evaluate(order, new PreparedKimbapData())
                : Evaluate(order, rows[rows.Count - 1]);
        }

        public static PreparedKimbapData ToPreparedKimbapData(KimbapResultRow row)
        {
            PreparedKimbapData prepared = new PreparedKimbapData();
            if (row == null)
            {
                return prepared;
            }

            AddItems(prepared.seaweeds, row.seaweedItems, KitchenIngredientCategory.Seaweed);
            AddItems(prepared.riceItems, row.riceItems, KitchenIngredientCategory.Rice);
            AddItems(prepared.fillings, row.fillingItems, KitchenIngredientCategory.Filling);
            return prepared;
        }

        private static void AddItems(List<PreparedKimbapItem> target, string serializedItems, KitchenIngredientCategory category)
        {
            if (string.IsNullOrWhiteSpace(serializedItems))
            {
                return;
            }

            string[] items = serializedItems.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < items.Length; i++)
            {
                string[] parts = items[i].Split(new[] { ':' }, 3);
                IngredientType ingredientType = IngredientType.Ham;
                if (parts.Length > 0)
                {
                    Enum.TryParse(parts[0], true, out ingredientType);
                }

                string variantId = parts.Length > 1 ? parts[1] : ingredientType.ToString();
                string displayName = parts.Length > 2 ? parts[2] : GetCatalogDisplayName(variantId);
                if (string.IsNullOrWhiteSpace(displayName))
                {
                    displayName = variantId;
                }

                target.Add(new PreparedKimbapItem(ingredientType, category, variantId, displayName));
            }
        }

        private static float ScoreSeaweed(EvaluationProfile profile, PreparedKimbapData prepared)
        {
            if (prepared.seaweeds.Count == 0)
            {
                return 0f;
            }

            if (HasUnrequestedSpecial(prepared.seaweeds, profile))
            {
                return 0f;
            }

            if (string.IsNullOrEmpty(profile.requiredSeaweedToken))
            {
                return SeaweedScore;
            }

            return ContainsToken(prepared.seaweeds, profile.requiredSeaweedToken) ? SeaweedScore : SeaweedScore * 0.55f;
        }

        private static float ScoreRiceCombination(EvaluationProfile profile, PreparedKimbapData prepared)
        {
            if (prepared.riceItems.Count == 0)
            {
                return 0f;
            }

            if (profile.requiredRiceTokens.Count == 0)
            {
                return RiceCombinationScore;
            }

            int matched = CountMatchedTokens(profile.requiredRiceTokens, prepared.riceItems);
            float score = RiceCombinationScore * ((float)matched / profile.requiredRiceTokens.Count);
            if (matched == profile.requiredRiceTokens.Count && profile.requiredRiceTokens.Count > 1)
            {
                score -= EstimateRiceRatioPenalty(profile, prepared);
            }

            return Clamp(score, 0f, RiceCombinationScore);
        }

        private static float ScoreRiceAmount(EvaluationProfile profile, PreparedKimbapData prepared)
        {
            int actual = prepared.riceItems.Count;
            int expected = profile.expectedRiceCount;
            if (actual == 0)
            {
                return 0f;
            }

            if (expected <= 0)
            {
                return actual <= 2 ? RiceAmountScore : actual == 3 ? 7f : 4f;
            }

            float ratio = (float)actual / expected;
            if (ratio >= 0.8f && ratio <= 1.1f)
            {
                return RiceAmountScore;
            }

            if (ratio >= 0.5f && ratio <= 1.5f)
            {
                return 7f;
            }

            return 3f;
        }

        private static float ScoreFillings(EvaluationProfile profile, PreparedKimbapData prepared)
        {
            if (prepared.fillings.Count == 0)
            {
                return 0f;
            }

            if (HasForbiddenFilling(profile, prepared.fillings))
            {
                return FillingScore * 0.2f;
            }

            if (profile.requiredFillingTokens.Count == 0)
            {
                return FillingScore;
            }

            int matched = CountMatchedTokens(profile.requiredFillingTokens, prepared.fillings);
            return FillingScore * ((float)matched / profile.requiredFillingTokens.Count);
        }

        private static float ScorePrice(EvaluationProfile profile, PreparedKimbapData prepared)
        {
            if (profile.targetPrice <= 0)
            {
                return PriceScore;
            }

            int price = CalculatePrice(prepared);
            float overRatio = ((float)price - profile.targetPrice) / profile.targetPrice;
            if (overRatio <= 0f) return PriceScore;
            if (overRatio <= 0.05f) return 12f;
            if (overRatio <= 0.1f) return 8f;
            if (overRatio <= 0.2f) return 4f;
            return 0f;
        }

        private static float ScoreSpecialIngredients(EvaluationProfile profile, PreparedKimbapData prepared)
        {
            List<PreparedKimbapItem> specialItems = prepared.AllItems.Where(IsSpecialItem).ToList();
            if (profile.requiredSpecialTokens.Count == 0)
            {
                return specialItems.Count == 0 ? SpecialScore : 0f;
            }

            int matched = CountMatchedTokens(profile.requiredSpecialTokens, prepared.AllItems);
            return SpecialScore * ((float)matched / profile.requiredSpecialTokens.Count);
        }

        private static void ApplyCriticalFailureCap(KimbapEvaluationResult result, EvaluationProfile profile, PreparedKimbapData prepared)
        {
            if (result == null || profile == null || prepared == null)
            {
                return;
            }

            if (HasMissingRequiredSpecial(profile, prepared) || HasUnrequestedSpecial(prepared.AllItems, profile))
            {
                result.baseScore = Math.Min(result.baseScore, CriticalFailureScoreCap);
            }
        }

        private static bool HasMissingRequiredSpecial(EvaluationProfile profile, PreparedKimbapData prepared)
        {
            return profile.requiredSpecialTokens.Count > 0
                && CountMatchedTokens(profile.requiredSpecialTokens, prepared.AllItems) < profile.requiredSpecialTokens.Count;
        }

        private static float ScoreAssembly(PreparedKimbapData prepared)
        {
            if (prepared.seaweeds.Count == 0 || prepared.riceItems.Count == 0 || prepared.fillings.Count == 0)
            {
                return 3f;
            }

            int totalItems = prepared.seaweeds.Count + prepared.riceItems.Count + prepared.fillings.Count;
            return totalItems > 16 ? 6f : AssemblyScore;
        }

        private static float ScoreDetails(EvaluationProfile profile, PreparedKimbapData prepared)
        {
            float score = DetailScore;
            if (profile.requiresExactOrder && !HasBasicOrder(prepared)) score -= 4f;
            if (profile.requiredFillingTokens.Count > 0 && CountMatchedTokens(profile.requiredFillingTokens, prepared.fillings) < profile.requiredFillingTokens.Count) score -= 3f;
            if (profile.requiredRiceTokens.Count > 0 && CountMatchedTokens(profile.requiredRiceTokens, prepared.riceItems) < profile.requiredRiceTokens.Count) score -= 2f;
            return Clamp(score, 0f, DetailScore);
        }

        private static bool HasBasicOrder(PreparedKimbapData prepared)
        {
            return prepared.seaweeds.Count > 0 && prepared.riceItems.Count > 0 && prepared.fillings.Count > 0;
        }

        private static int CountMatchedTokens(IReadOnlyList<string> requiredTokens, IEnumerable<PreparedKimbapItem> items)
        {
            int matched = 0;
            foreach (string token in requiredTokens)
            {
                if (ContainsToken(items, token))
                {
                    matched++;
                }
            }

            return matched;
        }

        private static bool ContainsToken(IEnumerable<PreparedKimbapItem> items, string token)
        {
            foreach (PreparedKimbapItem item in items)
            {
                if (ItemContains(item, token))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ItemContains(PreparedKimbapItem item, string token)
        {
            if (item == null || string.IsNullOrEmpty(token))
            {
                return false;
            }

            List<string> itemValues = GetItemSearchValues(item);
            List<string> tokenValues = GetTokenSearchValues(token);
            for (int i = 0; i < tokenValues.Count; i++)
            {
                string normalizedToken = Normalize(tokenValues[i]);
                if (string.IsNullOrEmpty(normalizedToken))
                {
                    continue;
                }

                for (int j = 0; j < itemValues.Count; j++)
                {
                    if (ContainsNormalized(itemValues[j], normalizedToken))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static List<string> GetItemSearchValues(PreparedKimbapItem item)
        {
            List<string> values = new List<string>
            {
                item.variantId,
                item.displayName,
                item.ingredientType.ToString(),
                ToToken(item.ingredientType)
            };

            AddCatalogAliases(values, item.variantId);
            AddCatalogAliases(values, item.displayName);
            return values;
        }

        private static List<string> GetTokenSearchValues(string token)
        {
            List<string> values = new List<string>();
            AddUniqueRaw(values, token);

            string normalizedToken = Normalize(token);
            if (TokenAliases.TryGetValue(normalizedToken, out string[] aliases))
            {
                for (int i = 0; i < aliases.Length; i++)
                {
                    AddUniqueRaw(values, aliases[i]);
                }
            }

            AddCatalogAliases(values, token);
            return values;
        }

        private static void AddCatalogAliases(List<string> values, string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            string normalizedToken = Normalize(token);
            foreach (CatalogIngredient ingredient in GetCatalogIngredients())
            {
                if (ingredient.ContainsAlias(normalizedToken))
                {
                    for (int i = 0; i < ingredient.aliases.Count; i++)
                    {
                        AddUniqueRaw(values, ingredient.aliases[i]);
                    }
                }
            }
        }

        private static void AddUniqueRaw(List<string> values, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            string normalizedValue = Normalize(value);
            for (int i = 0; i < values.Count; i++)
            {
                if (Normalize(values[i]) == normalizedValue)
                {
                    return;
                }
            }

            values.Add(value);
        }

        private static bool ContainsNormalized(string value, string normalizedToken)
        {
            return Normalize(value).IndexOf(normalizedToken, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool HasUnrequestedSpecial(IEnumerable<PreparedKimbapItem> items, EvaluationProfile profile)
        {
            foreach (PreparedKimbapItem item in items)
            {
                if (!IsSpecialItem(item))
                {
                    continue;
                }

                if (!profile.requiredSpecialTokens.Any(token => ItemContains(item, token)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasForbiddenFilling(EvaluationProfile profile, IEnumerable<PreparedKimbapItem> items)
        {
            foreach (PreparedKimbapItem item in items)
            {
                if (profile.forbiddenTokens.Any(token => ItemContains(item, token)))
                {
                    return true;
                }

                if (IsSpecialItem(item) && HasUnrequestedSpecial(new[] { item }, profile))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSpecialItem(PreparedKimbapItem item)
        {
            return ItemContains(item, "foil") || ItemContains(item, "stone");
        }

        private static float EstimateRiceRatioPenalty(EvaluationProfile profile, PreparedKimbapData prepared)
        {
            if (profile.riceRatioTokens.Count == 0 || prepared.riceItems.Count == 0)
            {
                return 0f;
            }

            float penalty = 0f;
            foreach (KeyValuePair<string, float> pair in profile.riceRatioTokens)
            {
                int count = prepared.riceItems.Count(item => ItemContains(item, pair.Key));
                float actualRatio = (float)count / prepared.riceItems.Count;
                penalty += Math.Abs(actualRatio - pair.Value) * 4f;
            }

            return Clamp(penalty, 0f, 4f);
        }

        private static int CalculatePrice(PreparedKimbapData prepared)
        {
            int total = 0;
            foreach (PreparedKimbapItem item in prepared.AllItems)
            {
                total += GetItemPrice(item);
            }

            return total;
        }

        private static int GetItemPrice(PreparedKimbapItem item)
        {
            if (item == null) return 0;
            if (IsSpecialItem(item)) return 1200;

            switch (item.category)
            {
                case KitchenIngredientCategory.Seaweed:
                    return 500;
                case KitchenIngredientCategory.Rice:
                    return 400;
                case KitchenIngredientCategory.Filling:
                    return item.ingredientType == IngredientType.Tuna || item.ingredientType == IngredientType.CrabMeat ? 900 : 600;
                default:
                    return 0;
            }
        }

        private static string ToToken(IngredientType ingredientType)
        {
            switch (ingredientType)
            {
                case IngredientType.Seaweed: return "seaweed";
                case IngredientType.Rice: return "rice";
                case IngredientType.Ham: return "ham";
                case IngredientType.Egg: return "egg";
                case IngredientType.Carrot: return "carrot";
                case IngredientType.Spinach: return "spinach";
                case IngredientType.Tuna: return "tuna";
                case IngredientType.CrabMeat: return "crabmeat";
                case IngredientType.PickledRadish: return "pickledradish";
                case IngredientType.GenericFilling: return "filling";
                default: return ingredientType.ToString();
            }
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            return value > max ? max : value;
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Trim().Replace(" ", string.Empty).Replace("-", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
        }

        private sealed class EvaluationProfile
        {
            public float maxScore = KimbapEvaluationResult.BaseMaxScore;
            public float targetPrice;
            public string requiredSeaweedToken;
            public int expectedRiceCount;
            public bool requiresExactOrder;
            public readonly List<string> requiredRiceTokens = new List<string>();
            public readonly List<string> requiredFillingTokens = new List<string>();
            public readonly List<string> requiredSpecialTokens = new List<string>();
            public readonly List<string> forbiddenTokens = new List<string>();
            public readonly Dictionary<string, float> riceRatioTokens = new Dictionary<string, float>();

            public static EvaluationProfile FromOrder(SheetOrderData order)
            {
                EvaluationProfile profile = new EvaluationProfile();
                string text = Join(order.customerName, order.orderDialogue, order.hintDialogue, order.seaweedName, order.riceName, order.fillingName);
                profile.maxScore = ParseMaxScore(text);
                profile.targetPrice = ParseTargetPrice(text);
                profile.requiredSeaweedToken = ParseSeaweedToken(text, order.seaweedName);
                profile.expectedRiceCount = order.riceCount;
                profile.requiresExactOrder = ContainsAny(text, "순서", "차례", "먼저", "order");
                AddRiceRequirements(profile, text, order.riceName);
                AddFillingRequirements(profile, text, order.fillingName, order.ingredients);
                AddSpecialRequirements(profile, text);
                AddForbiddenRequirements(profile, text);
                AddRiceRatioRequirements(profile, text);
                return profile;
            }

            private static string Join(params string[] values)
            {
                return string.Join(" ", values.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray());
            }

            private static float ParseMaxScore(string text)
            {
                float score = ParseNumberBeforeKeyword(text, "만점");
                return score > 0f ? score : KimbapEvaluationResult.BaseMaxScore;
            }

            private static float ParseTargetPrice(string text)
            {
                float wonPrice = ParseNumberBeforeKeyword(text, "원");
                if (wonPrice > 0f) return wonPrice;

                string normalized = Normalize(text).Replace(",", string.Empty);
                string digits = new string(normalized.Where(char.IsDigit).ToArray());
                if (digits.Length >= 4 && float.TryParse(digits, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
                {
                    return parsed;
                }

                return 0f;
            }

            private static float ParseNumberBeforeKeyword(string text, string keyword)
            {
                string normalized = Normalize(text).Replace(",", string.Empty);
                int keywordIndex = normalized.IndexOf(Normalize(keyword), StringComparison.OrdinalIgnoreCase);
                if (keywordIndex < 0) return 0f;

                int start = keywordIndex - 1;
                while (start >= 0 && char.IsDigit(normalized[start]))
                {
                    start--;
                }

                string number = normalized.Substring(start + 1, keywordIndex - start - 1);
                return float.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) ? parsed : 0f;
            }

            private static string ParseSeaweedToken(string text, string seaweedName)
            {
                string source = Join(text, seaweedName);
                if (ContainsAny(source, "호일", "알루미늄 호일", "알류미늄 호일", "aluminum foil", "aluminium foil", "foil")) return "foil";
                if (ContainsAny(source, "참기름", "sesame")) return "sesame";
                if (ContainsAny(source, "구운", "굽", "roasted")) return "roasted";
                if (ContainsAny(source, "적신", "젖은", "wet")) return "wet";
                if (ContainsAny(source, "기본", "plain", "김")) return "seaweed";
                return string.Empty;
            }

            private static void AddRiceRequirements(EvaluationProfile profile, string text, string riceName)
            {
                string source = Join(text, riceName);
                AddIfMentioned(profile.requiredRiceTokens, source, "white", "흰밥", "쌀밥", "백미", "white", "white rice");
                AddIfMentioned(profile.requiredRiceTokens, source, "brown", "현미", "brown", "brown rice");
                AddIfMentioned(profile.requiredRiceTokens, source, "black", "흑미", "black", "black rice");
                AddIfMentioned(profile.requiredRiceTokens, source, "seasoned", "양념밥", "seasoned");
                AddIfMentioned(profile.requiredRiceTokens, source, "spicy", "매운밥", "spicy");
            }

            private static void AddFillingRequirements(EvaluationProfile profile, string text, string fillingName, List<IngredientType> ingredients)
            {
                string source = Join(text, fillingName);
                AddIfMentioned(profile.requiredFillingTokens, source, "ham", "햄", "ham");
                AddIfMentioned(profile.requiredFillingTokens, source, "egg", "계란", "달걀", "egg");
                AddIfMentioned(profile.requiredFillingTokens, source, "carrot", "당근", "carrot");
                AddIfMentioned(profile.requiredFillingTokens, source, "spinach", "시금치", "spinach");
                AddIfMentioned(profile.requiredFillingTokens, source, "pickledradish", "단무지", "pickled radish", "radish");
                AddIfMentioned(profile.requiredFillingTokens, source, "tuna", "참치", "tuna");
                AddIfMentioned(profile.requiredFillingTokens, source, "crabmeat", "맛살", "게맛살", "크래미", "crab");
                AddIfMentioned(profile.requiredFillingTokens, source, "stone", "돌멩이", "돌맹이", "돌", "stone", "rock");
                AddCatalogFillingRequirements(profile, source);

                if (ingredients == null) return;
                foreach (IngredientType ingredient in ingredients)
                {
                    if (ingredient == IngredientType.Seaweed || ingredient == IngredientType.Rice) continue;
                    if (ingredient == IngredientType.GenericFilling) continue;
                    AddUnique(profile.requiredFillingTokens, ToToken(ingredient));
                }
            }

            private static void AddSpecialRequirements(EvaluationProfile profile, string text)
            {
                AddIfMentioned(profile.requiredSpecialTokens, text, "foil", "호일", "알루미늄 호일", "알류미늄 호일", "aluminum foil", "aluminium foil", "foil");
                AddIfMentioned(profile.requiredSpecialTokens, text, "stone", "돌멩이", "돌맹이", "돌", "stone", "rock");
            }

            private static void AddForbiddenRequirements(EvaluationProfile profile, string text)
            {
                if (ContainsAny(text, "오이제외", "오이 빼", "오이빼", "오이 넣지", "no cucumber"))
                {
                    AddUnique(profile.forbiddenTokens, "cucumber");
                    AddUnique(profile.forbiddenTokens, "오이");
                }
            }

            private static void AddRiceRatioRequirements(EvaluationProfile profile, string text)
            {
                if (ContainsAny(text, "7:3", "70%"))
                {
                    profile.riceRatioTokens["white"] = 0.7f;
                    profile.riceRatioTokens["brown"] = 0.3f;
                }

                if (ContainsAny(text, "6:4", "60%"))
                {
                    profile.riceRatioTokens["white"] = 0.6f;
                    profile.riceRatioTokens["brown"] = 0.4f;
                }
            }

            private static void AddIfMentioned(List<string> target, string source, string token, params string[] keywords)
            {
                if (ContainsAny(source, keywords))
                {
                    AddUnique(target, token);
                }
            }

            private static void AddCatalogFillingRequirements(EvaluationProfile profile, string source)
            {
                foreach (CatalogIngredient ingredient in GetCatalogIngredients())
                {
                    if (!ingredient.IsFilling)
                    {
                        continue;
                    }

                    for (int i = 0; i < ingredient.aliases.Count; i++)
                    {
                        if (ContainsAny(source, ingredient.aliases[i]))
                        {
                            AddUnique(profile.requiredFillingTokens, ingredient.id);
                            break;
                        }
                    }
                }
            }

            private static void AddUnique(List<string> target, string token)
            {
                string normalized = Normalize(token);
                if (!target.Contains(normalized))
                {
                    target.Add(normalized);
                }
            }

            private static bool ContainsAny(string source, params string[] keywords)
            {
                string normalizedSource = Normalize(source);
                foreach (string keyword in keywords)
                {
                    if (normalizedSource.IndexOf(Normalize(keyword), StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private static string GetCatalogDisplayName(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return string.Empty;
            }

            string normalizedId = Normalize(id);
            foreach (CatalogIngredient ingredient in GetCatalogIngredients())
            {
                if (Normalize(ingredient.id) == normalizedId)
                {
                    return ingredient.displayName;
                }
            }

            return string.Empty;
        }

        private static List<CatalogIngredient> GetCatalogIngredients()
        {
            if (catalogIngredients != null)
            {
                return catalogIngredients;
            }

            catalogIngredients = LoadCatalogIngredients();
            return catalogIngredients;
        }

        private static List<CatalogIngredient> LoadCatalogIngredients()
        {
            string path = FindIngredientsCsvPath();
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return new List<CatalogIngredient>();
            }

            try
            {
                List<List<string>> rows = CsvTableParser.Parse(File.ReadAllText(path));
                if (rows.Count <= 1)
                {
                    return new List<CatalogIngredient>();
                }

                Dictionary<string, int> header = CsvTableParser.BuildHeader(rows[0]);
                List<CatalogIngredient> ingredients = new List<CatalogIngredient>();
                for (int i = 1; i < rows.Count; i++)
                {
                    List<string> row = rows[i];
                    if (CsvTableParser.IsEmptyRow(row))
                    {
                        continue;
                    }

                    string id = CsvTableParser.GetAny(row, header, "Index");
                    string displayName = CsvTableParser.GetAny(row, header, "이름", "Name");
                    string category = CsvTableParser.GetAny(row, header, "분류", "Category");
                    if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(displayName))
                    {
                        continue;
                    }

                    ingredients.Add(new CatalogIngredient(id, displayName, category));
                }

                return ingredients;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to load ingredient aliases for evaluation: {exception.Message}");
                return new List<CatalogIngredient>();
            }
        }

        private static string FindIngredientsCsvPath()
        {
            List<string> candidates = new List<string>();
            if (!string.IsNullOrWhiteSpace(Application.streamingAssetsPath))
            {
                candidates.Add(Path.Combine(Application.streamingAssetsPath, IngredientsCsvRelativePath));
            }

            candidates.Add(Path.Combine(Environment.CurrentDirectory, "Assets", "StreamingAssets", IngredientsCsvRelativePath));
            candidates.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "StreamingAssets", IngredientsCsvRelativePath));

            for (int i = 0; i < candidates.Count; i++)
            {
                if (File.Exists(candidates[i]))
                {
                    return candidates[i];
                }
            }

            return string.Empty;
        }

        private sealed class CatalogIngredient
        {
            public readonly string id;
            public readonly string displayName;
            public readonly string category;
            public readonly List<string> aliases = new List<string>();

            public CatalogIngredient(string id, string displayName, string category)
            {
                this.id = id ?? string.Empty;
                this.displayName = displayName ?? string.Empty;
                this.category = category ?? string.Empty;
                AddAlias(id);
                AddAlias(displayName);
            }

            public bool IsFilling => Normalize(category) == "속" || Normalize(category) == "filling";

            public void AddAlias(string value)
            {
                AddUniqueRaw(aliases, value);
            }

            public bool ContainsAlias(string normalizedToken)
            {
                if (string.IsNullOrEmpty(normalizedToken))
                {
                    return false;
                }

                for (int i = 0; i < aliases.Count; i++)
                {
                    string normalizedAlias = Normalize(aliases[i]);
                    if (string.IsNullOrEmpty(normalizedAlias))
                    {
                        continue;
                    }

                    if (IsCatalogId(normalizedAlias) || IsCatalogId(normalizedToken))
                    {
                        if (normalizedAlias == normalizedToken)
                        {
                            return true;
                        }

                        continue;
                    }

                    if (normalizedAlias == normalizedToken
                        || normalizedAlias.IndexOf(normalizedToken, StringComparison.OrdinalIgnoreCase) >= 0
                        || normalizedToken.IndexOf(normalizedAlias, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }

                return false;
            }

            private static bool IsCatalogId(string value)
            {
                if (string.IsNullOrEmpty(value) || value[0] != 'r' || value.Length < 2)
                {
                    return false;
                }

                for (int i = 1; i < value.Length; i++)
                {
                    if (!char.IsDigit(value[i]))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
