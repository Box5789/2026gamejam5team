using System;

namespace KimbapGame.Evaluation
{
    [Serializable]
    public sealed class KimbapEvaluationCategoryScore
    {
        public string categoryName;
        public float baseMaxScore;
        public float baseScore;
        public string note;

        public float Ratio
        {
            get { return baseMaxScore <= 0f ? 0f : baseScore / baseMaxScore; }
        }

        public KimbapEvaluationCategoryScore(string categoryName, float baseMaxScore, float baseScore, string note)
        {
            this.categoryName = categoryName;
            this.baseMaxScore = baseMaxScore;
            this.baseScore = Clamp(baseScore, 0f, baseMaxScore);
            this.note = note ?? string.Empty;
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
