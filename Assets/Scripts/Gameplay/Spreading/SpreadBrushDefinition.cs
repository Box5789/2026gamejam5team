using System;
using UnityEngine;

namespace GameJam.Gameplay.Spreading
{
    [Serializable]
    public sealed class SpreadBrushDefinition
    {
        public const float DefaultBrushRadius = 0.18f;
        public const int DefaultParticlesPerSample = 4;
        public const float DefaultScatterRadius = 0.22f;
        public const float DefaultMinScale = 0.75f;
        public const float DefaultMaxScale = 1.25f;
        public const float DefaultStampSpacing = 0.04f;

        [SerializeField] private string id = "white-rice";
        [SerializeField] private string displayName = "White Rice";
        [SerializeField] private Texture2D brushTexture;
        [SerializeField] private Color tint = Color.white;
        [SerializeField, Min(0.01f)] private float brushRadius = DefaultBrushRadius;
        [SerializeField, Min(1)] private int particlesPerSample = DefaultParticlesPerSample;
        [SerializeField, Min(0f)] private float scatterRadius = DefaultScatterRadius;
        [SerializeField, Min(0.01f)] private float minScale = DefaultMinScale;
        [SerializeField, Min(0.01f)] private float maxScale = DefaultMaxScale;
        [SerializeField, Min(0f)] private float stampSpacing = DefaultStampSpacing;

        public SpreadBrushDefinition()
        {
        }

        public SpreadBrushDefinition(string id, string displayName, Texture2D brushTexture, Color tint)
            : this(
                id,
                displayName,
                brushTexture,
                tint,
                DefaultBrushRadius,
                DefaultParticlesPerSample,
                DefaultScatterRadius,
                DefaultMinScale,
                DefaultMaxScale,
                DefaultStampSpacing)
        {
        }

        public SpreadBrushDefinition(
            string id,
            string displayName,
            Texture2D brushTexture,
            Color tint,
            float brushRadius,
            int particlesPerSample,
            float scatterRadius,
            float minScale,
            float maxScale,
            float stampSpacing)
        {
            this.id = id;
            this.displayName = displayName;
            this.brushTexture = brushTexture;
            this.tint = tint;
            this.brushRadius = brushRadius;
            this.particlesPerSample = particlesPerSample;
            this.scatterRadius = scatterRadius;
            this.minScale = minScale;
            this.maxScale = maxScale;
            this.stampSpacing = stampSpacing;
            NormalizeValues();
        }

        public string Id => string.IsNullOrWhiteSpace(id) ? displayName : id;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? Id : displayName;

        public Texture2D BrushTexture => brushTexture;

        public Color Tint => tint;

        public float BrushRadius => brushRadius;

        public int ParticlesPerSample => particlesPerSample;

        public float ScatterRadius => scatterRadius;

        public float MinScale => minScale;

        public float MaxScale => maxScale;

        public float StampSpacing => stampSpacing;

        public void NormalizeValues()
        {
            brushRadius = Mathf.Max(0.01f, brushRadius);
            particlesPerSample = Mathf.Max(1, particlesPerSample);
            scatterRadius = Mathf.Max(0f, scatterRadius);
            minScale = Mathf.Max(0.01f, minScale);
            maxScale = Mathf.Max(0.01f, maxScale);

            if (minScale > maxScale)
            {
                float previousMinScale = minScale;
                minScale = maxScale;
                maxScale = previousMinScale;
            }

            stampSpacing = Mathf.Max(0f, stampSpacing);
        }
    }
}
