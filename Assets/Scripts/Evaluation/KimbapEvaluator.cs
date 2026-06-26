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
                result.AddCategory("주문 데이터", KimbapEvaluationResult.BaseMaxScore, 0f, "주문 데이터가 없습니다.");
                result.FinalizeScore(KimbapEvaluationResult.BaseMaxScore, string.Empty, string.Empty, "주문 데이터가 없어서 평가할 수 없습니다.", string.Empty);
                return result;
            }

            PreparedKimbapData prepared = preparedKimbap ?? new PreparedKimbapData();
            EvaluationProfile profile = EvaluationProfile.FromOrder(order);

            result.AddCategory("김 상태", SeaweedScore, ScoreSeaweed(profile, prepared, out string seaweedNote), seaweedNote);
            result.AddCategory("밥 조합", RiceCombinationScore, ScoreRiceCombination(profile, prepared, out string riceCombinationNote), riceCombinationNote);
            result.AddCategory("밥 양", RiceAmountScore, ScoreRiceAmount(profile, prepared, out string riceAmountNote), riceAmountNote);
            result.AddCategory("속재료 요구", FillingScore, ScoreFillings(profile, prepared, out string fillingNote), fillingNote);
            result.AddCategory("가격 요구", PriceScore, ScorePrice(profile, prepared, out string priceNote), priceNote);
            result.AddCategory("특수 재료/전용 재료", SpecialScore, ScoreSpecialIngredients(profile, prepared, out string specialNote), specialNote);
            result.AddCategory("조립 완성도", AssemblyScore, ScoreAssembly(prepared, out string assemblyNote), assemblyNote);
            result.AddCategory("요구 조건 반영", DetailScore, ScoreDetails(profile, prepared, out string detailNote), detailNote);
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


        private static float ScoreSeaweed(EvaluationProfile profile, PreparedKimbapData prepared, out string note)
        {
            if (prepared.seaweeds.Count == 0)
            {
                note = "김이 없습니다.";
                return 0f;
            }

            bool hasWrongSpecial = HasUnrequestedSpecial(prepared.seaweeds, profile);
            if (hasWrongSpecial)
            {
                note = "요구하지 않은 전용 김 재료가 들어갔습니다.";
                return 0f;
            }

            if (string.IsNullOrEmpty(profile.requiredSeaweedToken))
            {
                note = "김이 들어갔습니다.";
                return SeaweedScore;
            }

            if (ContainsToken(prepared.seaweeds, profile.requiredSeaweedToken))
            {
                note = "주문한 김 상태와 맞습니다.";
                return SeaweedScore;
            }

            note = "김 종류는 있으나 상태가 다릅니다.";
            return SeaweedScore * 0.55f;
        }

        private static float ScoreRiceCombination(EvaluationProfile profile, PreparedKimbapData prepared, out string note)
        {
            if (prepared.riceItems.Count == 0)
            {
                note = "밥이 없습니다.";
                return 0f;
            }

            if (profile.requiredRiceTokens.Count == 0)
            {
                note = "밥이 들어갔습니다.";
                return RiceCombinationScore;
            }

            int matched = CountMatchedTokens(profile.requiredRiceTokens, prepared.riceItems);
            float score = RiceCombinationScore * ((float)matched / profile.requiredRiceTokens.Count);
            if (matched == profile.requiredRiceTokens.Count && profile.requiredRiceTokens.Count > 1)
            {
                score -= EstimateRiceRatioPenalty(profile, prepared);
            }

            note = matched == profile.requiredRiceTokens.Count
                ? "요구한 밥 종류를 모두 사용했습니다."
                : "요구한 밥 종류 일부가 빠졌습니다.";
            return Clamp(score, 0f, RiceCombinationScore);
        }

        private static float ScoreRiceAmount(EvaluationProfile profile, PreparedKimbapData prepared, out string note)
        {
            int count = prepared.riceItems.Count;
            if (count == 0)
            {
                note = "밥 양이 0입니다.";
                return 0f;
            }

            if (profile.riceAmount == RiceAmountPreference.Thin)
            {
                note = count <= 1 ? "얇은 밥 양에 가깝습니다." : "밥이 요청보다 많습니다.";
                return count <= 1 ? RiceAmountScore : count == 2 ? 7f : 4f;
            }

            if (profile.riceAmount == RiceAmountPreference.Heavy)
            {
                note = count >= 3 ? "넉넉한 밥 양에 가깝습니다." : "밥이 요청보다 적습니다.";
                return count >= 3 ? RiceAmountScore : count == 2 ? 7f : 4f;
            }

            note = count <= 2 ? "보통 밥 양입니다." : "밥이 약간 많습니다.";
            return count <= 2 ? RiceAmountScore : count == 3 ? 7f : 4f;
        }

        private static float ScoreFillings(EvaluationProfile profile, PreparedKimbapData prepared, out string note)
        {
            if (prepared.fillings.Count == 0)
            {
                note = "속재료가 없습니다.";
                return 0f;
            }

            if (HasForbiddenFilling(profile, prepared.fillings))
            {
                note = "금지 또는 요구하지 않은 특수 속재료가 들어갔습니다.";
                return FillingScore * 0.2f;
            }

            if (profile.requiredFillingTokens.Count == 0)
            {
                note = "속재료가 들어갔습니다.";
                return FillingScore;
            }

            int matched = CountMatchedTokens(profile.requiredFillingTokens, prepared.fillings);
            float ratio = (float)matched / profile.requiredFillingTokens.Count;
            note = matched == profile.requiredFillingTokens.Count
                ? "요구한 핵심 속재료를 모두 넣었습니다."
                : "요구한 핵심 속재료 일부가 빠졌습니다.";
            return FillingScore * ratio;
        }

        private static float ScorePrice(EvaluationProfile profile, PreparedKimbapData prepared, out string note)
        {
            if (profile.targetPrice <= 0)
            {
                note = "가격 요구가 없어 만점 처리합니다.";
                return PriceScore;
            }

            int price = CalculatePrice(prepared);
            float overRatio = ((float)price - profile.targetPrice) / profile.targetPrice;
            if (overRatio <= 0f)
            {
                note = $"목표 가격 {profile.targetPrice.ToString(CultureInfo.InvariantCulture)}원 이하입니다.";
                return PriceScore;
            }

            if (overRatio <= 0.05f)
            {
                note = "목표 가격을 5% 이내로 초과했습니다.";
                return 12f;
            }

            if (overRatio <= 0.1f)
            {
                note = "목표 가격을 10% 이내로 초과했습니다.";
                return 8f;
            }

            if (overRatio <= 0.2f)
            {
                note = "목표 가격을 20% 이내로 초과했습니다.";
                return 4f;
            }

            note = "예산을 크게 초과했습니다.";
            return 0f;
        }

        private static float ScoreSpecialIngredients(EvaluationProfile profile, PreparedKimbapData prepared, out string note)
        {
            List<PreparedKimbapItem> specialItems = prepared.AllItems.Where(IsSpecialItem).ToList();
            if (profile.requiredSpecialTokens.Count == 0)
            {
                note = specialItems.Count == 0 ? "요구하지 않은 전용 재료가 없습니다." : "요구하지 않은 전용 재료가 들어갔습니다.";
                return specialItems.Count == 0 ? SpecialScore : 0f;
            }

            int matched = CountMatchedTokens(profile.requiredSpecialTokens, prepared.AllItems);
            note = matched == profile.requiredSpecialTokens.Count
                ? "요구한 전용 재료를 사용했습니다."
                : "요구한 전용 재료가 빠졌습니다.";
            return SpecialScore * ((float)matched / profile.requiredSpecialTokens.Count);
        }

        private static float ScoreAssembly(PreparedKimbapData prepared, out string note)
        {
            if (prepared.seaweeds.Count == 0 || prepared.riceItems.Count == 0 || prepared.fillings.Count == 0)
            {
                note = "김밥 구성 요소가 빠져 완성도가 낮습니다.";
                return 3f;
            }

            int totalItems = prepared.seaweeds.Count + prepared.riceItems.Count + prepared.fillings.Count;
            if (totalItems > 16)
            {
                note = "재료가 너무 많아 삐져나올 가능성이 큽니다.";
                return 6f;
            }

            note = "현재 데이터상 조립 문제가 없습니다.";
            return AssemblyScore;
        }

        private static float ScoreDetails(EvaluationProfile profile, PreparedKimbapData prepared, out string note)
        {
            float score = DetailScore;
            if (profile.requiresExactOrder && !HasBasicOrder(prepared))
            {
                score -= 4f;
            }

            if (profile.requiredFillingTokens.Count > 0 && CountMatchedTokens(profile.requiredFillingTokens, prepared.fillings) < profile.requiredFillingTokens.Count)
            {
                score -= 3f;
            }

            if (profile.requiredRiceTokens.Count > 0 && CountMatchedTokens(profile.requiredRiceTokens, prepared.riceItems) < profile.requiredRiceTokens.Count)
            {
                score -= 2f;
            }

            note = score >= DetailScore ? "세부 요구를 반영했습니다." : "세부 요구 일부가 빠졌습니다.";
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
            string normalizedToken = Normalize(token);
            foreach (PreparedKimbapItem item in items)
            {
                if (ItemContains(item, normalizedToken))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ItemContains(PreparedKimbapItem item, string normalizedToken)
        {
            if (item == null || string.IsNullOrEmpty(normalizedToken))
            {
                return false;
            }

            return ContainsNormalized(item.variantId, normalizedToken)
                || ContainsNormalized(item.displayName, normalizedToken)
                || ContainsNormalized(item.ingredientType.ToString(), normalizedToken);
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

                bool requested = false;
                foreach (string token in profile.requiredSpecialTokens)
                {
                    if (ItemContains(item, Normalize(token)))
                    {
                        requested = true;
                        break;
                    }
                }

                if (!requested)
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
                foreach (string forbiddenToken in profile.forbiddenTokens)
                {
                    if (ItemContains(item, Normalize(forbiddenToken)))
                    {
                        return true;
                    }
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
            return ItemContains(item, "foil") || ItemContains(item, "호일") || ItemContains(item, "stone") || ItemContains(item, "rock") || ItemContains(item, "돌멩") || ItemContains(item, "돌");
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
                int count = prepared.riceItems.Count(item => ItemContains(item, Normalize(pair.Key)));
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
            if (item == null)
            {
                return 0;
            }

            if (IsSpecialItem(item))
            {
                return 1200;
            }

            switch (item.category)
            {
                case KitchenIngredientCategory.Seaweed:
                    return 500;
                case KitchenIngredientCategory.Rice:
                    return 400;
                case KitchenIngredientCategory.Filling:
                    if (ItemContains(item, "tuna") || ItemContains(item, "참치"))
                    {
                        return 900;
                    }

                    return 600;
                default:
                    return 0;
            }
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Trim().Replace(" ", string.Empty).Replace("-", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
        }

        private enum RiceAmountPreference
        {
            Normal,
            Thin,
            Heavy
        }

        private sealed class EvaluationProfile
        {
            public float maxScore = KimbapEvaluationResult.BaseMaxScore;
            public float targetPrice;
            public string requiredSeaweedToken;
            public RiceAmountPreference riceAmount = RiceAmountPreference.Normal;
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
                profile.riceAmount = ParseRiceAmount(text);
                profile.requiresExactOrder = ContainsAny(text, "순서", "차례", "먼저");
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
                if (score > 0f)
                {
                    return score;
                }

                if (ContainsAny(text, "낮은만점", "쉬운손님"))
                {
                    return 50f;
                }

                return KimbapEvaluationResult.BaseMaxScore;
            }

            private static float ParseTargetPrice(string text)
            {
                return ParseNumberBeforeKeyword(text, "원");
            }

            private static float ParseNumberBeforeKeyword(string text, string keyword)
            {
                string normalized = Normalize(text);
                int keywordIndex = normalized.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
                if (keywordIndex < 0)
                {
                    return 0f;
                }

                int start = keywordIndex - 1;
                while (start >= 0 && (char.IsDigit(normalized[start]) || normalized[start] == ','))
                {
                    start--;
                }

                string number = normalized.Substring(start + 1, keywordIndex - start - 1).Replace(",", string.Empty);
                return float.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) ? parsed : 0f;
            }

            private static string ParseSeaweedToken(string text, string seaweedName)
            {
                string source = Join(text, seaweedName);
                if (ContainsAny(source, "호일", "foil")) return "foil";
                if (ContainsAny(source, "참기름", "sesame")) return "sesame";
                if (ContainsAny(source, "구운", "굽", "roasted")) return "roasted";
                if (ContainsAny(source, "소금", "salted")) return "salted";
                if (ContainsAny(source, "두꺼운", "thick")) return "thick";
                if (ContainsAny(source, "기본", "plain")) return "plain";
                return string.Empty;
            }

            private static RiceAmountPreference ParseRiceAmount(string text)
            {
                if (ContainsAny(text, "얇게", "적게", "조금", "thin")) return RiceAmountPreference.Thin;
                if (ContainsAny(text, "많이", "두껍", "넉넉", "heavy")) return RiceAmountPreference.Heavy;
                return RiceAmountPreference.Normal;
            }

            private static void AddRiceRequirements(EvaluationProfile profile, string text, string riceName)
            {
                string source = Join(text, riceName);
                AddIfMentioned(profile.requiredRiceTokens, source, "white", "흰쌀", "흰밥", "쌀밥", "white rice");
                AddIfMentioned(profile.requiredRiceTokens, source, "brown", "현미", "brown rice");
                AddIfMentioned(profile.requiredRiceTokens, source, "black", "흑미", "black rice");
                AddIfMentioned(profile.requiredRiceTokens, source, "seasoned", "양념밥", "seasoned rice");
                AddIfMentioned(profile.requiredRiceTokens, source, "spicy", "매운밥", "spicy rice");
            }

            private static void AddFillingRequirements(EvaluationProfile profile, string text, string fillingName, List<IngredientType> ingredients)
            {
                string source = Join(text, fillingName);
                if (ContainsAny(source, "기본"))
                {
                    AddUnique(profile.requiredFillingTokens, "ham");
                    AddUnique(profile.requiredFillingTokens, "egg");
                    AddUnique(profile.requiredFillingTokens, "carrot");
                    AddUnique(profile.requiredFillingTokens, "spinach");
                    AddUnique(profile.requiredFillingTokens, "pickledradish");
                }

                AddIfMentioned(profile.requiredFillingTokens, source, "ham", "햄", "ham");
                AddIfMentioned(profile.requiredFillingTokens, source, "egg", "계란", "달걀", "egg");
                AddIfMentioned(profile.requiredFillingTokens, source, "carrot", "당근", "carrot");
                AddIfMentioned(profile.requiredFillingTokens, source, "spinach", "시금치", "spinach");
                AddIfMentioned(profile.requiredFillingTokens, source, "pickledradish", "단무지", "pickled radish", "radish");
                AddIfMentioned(profile.requiredFillingTokens, source, "tuna", "참치", "tuna");
                AddIfMentioned(profile.requiredFillingTokens, source, "crab", "맛살", "crab");
                AddIfMentioned(profile.requiredFillingTokens, source, "burdock", "우엉", "burdock");

                if (ingredients == null)
                {
                    return;
                }

                foreach (IngredientType ingredient in ingredients)
                {
                    if (ingredient == IngredientType.Seaweed || ingredient == IngredientType.Rice)
                    {
                        continue;
                    }

                    AddUnique(profile.requiredFillingTokens, Normalize(ingredient.ToString()));
                }
            }

            private static void AddSpecialRequirements(EvaluationProfile profile, string text)
            {
                AddIfMentioned(profile.requiredSpecialTokens, text, "foil", "호일", "foil");
                AddIfMentioned(profile.requiredSpecialTokens, text, "stone", "돌멩", "돌", "stone", "rock");
            }

            private static void AddForbiddenRequirements(EvaluationProfile profile, string text)
            {
                if (ContainsAny(text, "오이제외", "오이빼", "오이는빼", "no cucumber"))
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
