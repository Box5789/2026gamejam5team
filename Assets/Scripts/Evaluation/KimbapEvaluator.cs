using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using KimbapGame.Data;
using KimbapGame.Kitchen;

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
                string[] parts = items[i].Split(new[] { ':' }, 2);
                IngredientType ingredientType = IngredientType.Ham;
                if (parts.Length > 0)
                {
                    Enum.TryParse(parts[0], true, out ingredientType);
                }

                string variantId = parts.Length > 1 ? parts[1] : ingredientType.ToString();
                target.Add(new PreparedKimbapItem(ingredientType, category, variantId, variantId));
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

            string normalizedToken = Normalize(token);
            return ContainsNormalized(item.variantId, normalizedToken)
                || ContainsNormalized(item.displayName, normalizedToken)
                || ContainsNormalized(item.ingredientType.ToString(), normalizedToken)
                || ContainsNormalized(ToToken(item.ingredientType), normalizedToken);
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
            return ItemContains(item, "foil") || ItemContains(item, "호일") || ItemContains(item, "stone") || ItemContains(item, "rock") || ItemContains(item, "돌") || ItemContains(item, "돌멩이");
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
                if (ContainsAny(source, "호일", "foil")) return "foil";
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

                if (ingredients == null) return;
                foreach (IngredientType ingredient in ingredients)
                {
                    if (ingredient == IngredientType.Seaweed || ingredient == IngredientType.Rice) continue;
                    AddUnique(profile.requiredFillingTokens, ToToken(ingredient));
                }
            }

            private static void AddSpecialRequirements(EvaluationProfile profile, string text)
            {
                AddIfMentioned(profile.requiredSpecialTokens, text, "foil", "호일", "foil");
                AddIfMentioned(profile.requiredSpecialTokens, text, "stone", "돌멩이", "돌", "stone", "rock");
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
    }
}
