using System;
using System.Collections.Generic;

namespace KimbapGame.Evaluation
{
    [Serializable]
    public sealed class KimbapEvaluationResult
    {
        public const float BaseMaxScore = 100f;

        public float baseScore;
        public float customerMaxScore;
        public float finalScore;
        public float finalRatio;
        public KimbapEvaluationGrade grade;
        public bool isSuccess;
        public string responseDialogue;
        public string responseImageName;
        public readonly List<KimbapEvaluationCategoryScore> categoryScores = new List<KimbapEvaluationCategoryScore>();

        public void AddCategory(string categoryName, float maxScore, float score, string note)
        {
            KimbapEvaluationCategoryScore categoryScore = new KimbapEvaluationCategoryScore(categoryName, maxScore, score, note);
            categoryScores.Add(categoryScore);
            baseScore += categoryScore.baseScore;
        }

        public void FinalizeScore(float maxScore, string successDialogue, string successImageName, string failDialogue, string failImageName)
        {
            customerMaxScore = maxScore <= 0f ? BaseMaxScore : maxScore;
            baseScore = Clamp(baseScore, 0f, BaseMaxScore);
            finalScore = baseScore * (customerMaxScore / BaseMaxScore);
            finalRatio = customerMaxScore <= 0f ? 0f : finalScore / customerMaxScore;
            grade = ToGrade(finalRatio);
            isSuccess = finalRatio >= 0.6f;
            responseDialogue = isSuccess ? successDialogue : failDialogue;
            responseImageName = isSuccess ? successImageName : failImageName;
        }

        private static KimbapEvaluationGrade ToGrade(float ratio)
        {
            if (ratio >= 0.9f)
            {
                return KimbapEvaluationGrade.S;
            }

            if (ratio >= 0.75f)
            {
                return KimbapEvaluationGrade.A;
            }

            if (ratio >= 0.6f)
            {
                return KimbapEvaluationGrade.B;
            }

            if (ratio >= 0.4f)
            {
                return KimbapEvaluationGrade.C;
            }

            return KimbapEvaluationGrade.F;
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }
}
