using System;
using UnityEngine;

namespace GameJam.Gameplay.Spreading
{
    [Serializable]
    public sealed class SpreadBrushDefinition
    {
        [SerializeField] private string id = "white-rice";
        [SerializeField] private string displayName = "White Rice";
        [SerializeField] private Texture2D brushTexture;
        [SerializeField] private Color tint = Color.white;
        [SerializeField, Min(0.01f)] private float brushRadius = 0.18f;
        [SerializeField, Min(1)] private int particlesPerSample = 4;
        [SerializeField, Min(0f)] private float scatterRadius = 0.22f;
        [SerializeField, Min(0.01f)] private float minScale = 0.75f;
        [SerializeField, Min(0.01f)] private float maxScale = 1.25f;
        [SerializeField, Min(0f)] private float stampSpacing = 0.04f;

        public SpreadBrushDefinition()
        {
        }

        public SpreadBrushDefinition(string id, string displayName, Texture2D brushTexture, Color tint)
        {
            this.id = id;
            this.displayName = displayName;
            this.brushTexture = brushTexture;
            this.tint = tint;
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
