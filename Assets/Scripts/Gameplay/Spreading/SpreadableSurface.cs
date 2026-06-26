using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameJam.Gameplay.Spreading
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpreadableSurface : MonoBehaviour
    {
        private const byte MinimumStampAlpha = 8;

        [SerializeField, Min(8)] private int textureWidth = 256;
        [SerializeField, Min(8)] private int textureHeight = 256;
        [SerializeField, Range(0f, 1f)] private float requiredCoverage = 0.7f;
        [SerializeField] private Color surfaceColor = new Color(0.93f, 0.73f, 0.47f, 1f);
        [SerializeField] private SpreadSurfaceShape surfaceShape = SpreadSurfaceShape.Ellipse;
        [SerializeField] private bool paintOnlyWhilePointerIsOverSurface = true;
        [SerializeField, Min(0)] private int selectedBrushIndex;
        [SerializeField] private List<SpreadBrushDefinition> brushes = new List<SpreadBrushDefinition>
        {
            new SpreadBrushDefinition("white-rice", "White Rice", null, new Color(1f, 0.97f, 0.86f, 1f)),
            new SpreadBrushDefinition("seasoned-rice", "Seasoned Rice", null, new Color(0.98f, 0.86f, 0.62f, 1f))
        };

        private readonly System.Random random = new System.Random();
        private SpriteRenderer spriteRendererComponent;
        private SpreadMask spreadMask;
        private Texture2D spreadTexture;
        private Sprite generatedSprite;
        private Color32[] pixels;
        private BrushRuntimeState[] brushRuntimeStates = Array.Empty<BrushRuntimeState>();
        private Vector3[] lastPaintWorldPoints = Array.Empty<Vector3>();
        private bool[] hasLastPaintWorldPoints = Array.Empty<bool>();
        private SpreadBrushSelector brushSelector;

        public int TextureWidth => textureWidth;

        public int TextureHeight => textureHeight;

        public float RequiredCoverage => requiredCoverage;

        public SpreadSurfaceShape SurfaceShape => surfaceShape;

        public bool PaintOnlyWhilePointerIsOverSurface => paintOnlyWhilePointerIsOverSurface;

        public int BrushCount => brushes == null ? 0 : brushes.Count;

        public float Coverage => spreadMask == null ? 0f : spreadMask.Coverage;

        public bool IsComplete => spreadMask != null && spreadMask.IsComplete(requiredCoverage);

        private void Awake()
        {
            InitializeSurface();
        }

        private void OnDestroy()
        {
            DestroyGeneratedAssets();
        }

        private void OnValidate()
        {
            textureWidth = Mathf.Max(8, textureWidth);
            textureHeight = Mathf.Max(8, textureHeight);
            requiredCoverage = Mathf.Clamp01(requiredCoverage);
            EnsureBrushDefinitions();
            NormalizeBrushDefinitions();
            selectedBrushIndex = Mathf.Clamp(selectedBrushIndex, 0, Mathf.Max(0, BrushCount - 1));
        }

        public void InitializeSurface()
        {
            spriteRendererComponent = GetComponent<SpriteRenderer>();
            EnsureBrushDefinitions();
            NormalizeBrushDefinitions();
            brushSelector = new SpreadBrushSelector(selectedBrushIndex);
            brushSelector.ClampToCount(BrushCount);
            selectedBrushIndex = brushSelector.SelectedIndex;

            spreadMask = new SpreadMask(textureWidth, textureHeight, surfaceShape);
            pixels = new Color32[textureWidth * textureHeight];
            brushRuntimeStates = BuildBrushRuntimeStates();
            lastPaintWorldPoints = new Vector3[BrushCount];
            hasLastPaintWorldPoints = new bool[BrushCount];

            DestroyGeneratedAssets();

            spreadTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false)
            {
                name = $"{name}_SpreadTexture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            generatedSprite = Sprite.Create(
                spreadTexture,
                new Rect(0f, 0f, textureWidth, textureHeight),
                new Vector2(0.5f, 0.5f),
                textureWidth);
            generatedSprite.name = $"{name}_SpreadSprite";
            spriteRendererComponent.sprite = generatedSprite;

            ResetPixelsToSurface();
            ApplyTexture();
        }

        public bool SelectBrush(int index)
        {
            EnsureSelector();

            if (!brushSelector.SelectIndex(BrushCount, index))
            {
                return false;
            }

            selectedBrushIndex = brushSelector.SelectedIndex;
            return true;
        }

        public bool SelectBrush(string id)
        {
            EnsureSelector();

            if (!brushSelector.SelectId(brushes, id))
            {
                return false;
            }

            selectedBrushIndex = brushSelector.SelectedIndex;
            return true;
        }

        public bool SelectOrCreateBrush(string id, string displayName, Color tint)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            EnsureBrushDefinitions();

            int brushIndex = -1;
            for (int i = 0; i < brushes.Count; i++)
            {
                if (brushes[i] != null && string.Equals(brushes[i].Id, id, StringComparison.Ordinal))
                {
                    brushIndex = i;
                    break;
                }
            }

            SpreadBrushDefinition brushDefinition = new SpreadBrushDefinition(id, displayName, null, tint);
            if (brushIndex >= 0)
            {
                brushes[brushIndex] = brushDefinition;
            }
            else
            {
                brushes.Add(brushDefinition);
                brushIndex = brushes.Count - 1;
            }

            NormalizeBrushDefinitions();
            brushRuntimeStates = BuildBrushRuntimeStates();
            lastPaintWorldPoints = new Vector3[BrushCount];
            hasLastPaintWorldPoints = new bool[BrushCount];
            return SelectBrush(brushIndex);
        }

        public int GetSelectedBrushIndex()
        {
            EnsureSelector();
            return brushSelector.SelectedIndex;
        }

        public string GetSelectedBrushId()
        {
            SpreadBrushDefinition brush = GetSelectedBrush();
            return brush == null ? string.Empty : brush.Id;
        }

        public string GetSelectedBrushDisplayName()
        {
            SpreadBrushDefinition brush = GetSelectedBrush();
            return brush == null ? string.Empty : brush.DisplayName;
        }

        public bool PaintAtWorldPoint(Vector3 worldPoint)
        {
            return PaintAtWorldPoint(worldPoint, GetSelectedBrushIndex());
        }

        public bool PaintAtWorldPoint(Vector3 worldPoint, int brushIndex)
        {
            if (spreadMask == null || spreadTexture == null || spriteRendererComponent == null)
            {
                InitializeSurface();
            }

            if (!IsValidBrushIndex(brushIndex))
            {
                return false;
            }

            SpreadBrushDefinition brush = brushes[brushIndex];
            if (brush.StampSpacing > 0f
                && hasLastPaintWorldPoints[brushIndex]
                && Vector3.Distance(lastPaintWorldPoints[brushIndex], worldPoint) < brush.StampSpacing)
            {
                return false;
            }

            if (!TryWorldToNormalized(worldPoint, out float normalizedX, out float normalizedY))
            {
                return false;
            }

            if (paintOnlyWhilePointerIsOverSurface && !spreadMask.ContainsNormalizedPoint(normalizedX, normalizedY))
            {
                return false;
            }

            bool changed = PaintBrushParticles(worldPoint, normalizedX, normalizedY, brushIndex);
            if (!changed)
            {
                return false;
            }

            hasLastPaintWorldPoints[brushIndex] = true;
            lastPaintWorldPoints[brushIndex] = worldPoint;
            ApplyTexture();
            return true;
        }

        public void ResetSpread()
        {
            if (spreadMask == null || spreadTexture == null)
            {
                InitializeSurface();
                return;
            }

            spreadMask.Reset();
            Array.Clear(hasLastPaintWorldPoints, 0, hasLastPaintWorldPoints.Length);
            ResetPixelsToSurface();
            ApplyTexture();
        }

        private SpreadBrushDefinition GetSelectedBrush()
        {
            EnsureSelector();
            return IsValidBrushIndex(brushSelector.SelectedIndex) ? brushes[brushSelector.SelectedIndex] : null;
        }

        private void EnsureSelector()
        {
            EnsureBrushDefinitions();

            if (brushSelector == null)
            {
                brushSelector = new SpreadBrushSelector(selectedBrushIndex);
            }

            brushSelector.ClampToCount(BrushCount);
            selectedBrushIndex = brushSelector.SelectedIndex;
        }

        private bool PaintBrushParticles(Vector3 worldPoint, float normalizedX, float normalizedY, int brushIndex)
        {
            SpreadBrushDefinition brush = brushes[brushIndex];
            bool changed = false;

            for (int i = 0; i < brush.ParticlesPerSample; i++)
            {
                float particleNormalizedX = normalizedX;
                float particleNormalizedY = normalizedY;

                if (brush.ScatterRadius > 0f)
                {
                    GetNormalizedScatterOffset(worldPoint, brush.ScatterRadius, out float scatterX, out float scatterY);
                    particleNormalizedX += scatterX;
                    particleNormalizedY += scatterY;
                }

                if (paintOnlyWhilePointerIsOverSurface && !spreadMask.ContainsNormalizedPoint(particleNormalizedX, particleNormalizedY))
                {
                    continue;
                }

                float scale = Mathf.Lerp(brush.MinScale, brush.MaxScale, NextRandom01());
                changed |= StampParticle(particleNormalizedX, particleNormalizedY, brush.BrushRadius * scale, brushRuntimeStates[brushIndex]);
            }

            return changed;
        }

        private bool StampParticle(float normalizedX, float normalizedY, float radiusWorldUnits, BrushRuntimeState brushState)
        {
            if (radiusWorldUnits <= 0f)
            {
                return false;
            }

            GetNormalizedRadius(radiusWorldUnits, out float normalizedRadiusX, out float normalizedRadiusY);
            int centerX = Mathf.RoundToInt(normalizedX * (textureWidth - 1));
            int centerY = Mathf.RoundToInt(normalizedY * (textureHeight - 1));
            int radiusPixelsX = Mathf.Max(1, Mathf.CeilToInt(normalizedRadiusX * textureWidth));
            int radiusPixelsY = Mathf.Max(1, Mathf.CeilToInt(normalizedRadiusY * textureHeight));
            int minX = Mathf.Max(0, centerX - radiusPixelsX);
            int maxX = Mathf.Min(textureWidth - 1, centerX + radiusPixelsX);
            int minY = Mathf.Max(0, centerY - radiusPixelsY);
            int maxY = Mathf.Min(textureHeight - 1, centerY + radiusPixelsY);
            bool changed = false;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (!spreadMask.IsCoverable(x, y))
                    {
                        continue;
                    }

                    Color32 stampPixel = brushState.HasReadableTexture
                        ? SampleBrushTexture(brushState, x, y, minX, maxX, minY, maxY)
                        : SampleFallbackBrush(brushState, x, y, centerX, centerY, radiusPixelsX, radiusPixelsY);

                    if (stampPixel.a < MinimumStampAlpha)
                    {
                        continue;
                    }

                    int index = (y * textureWidth) + x;
                    pixels[index] = AlphaBlend(pixels[index], stampPixel);
                    spreadMask.PaintPixel(x, y);
                    changed = true;
                }
            }

            return changed;
        }

        private Color32 SampleBrushTexture(BrushRuntimeState brushState, int x, int y, int minX, int maxX, int minY, int maxY)
        {
            float u = maxX == minX ? 0.5f : Mathf.InverseLerp(minX, maxX, x);
            float v = maxY == minY ? 0.5f : Mathf.InverseLerp(minY, maxY, y);
            int sourceX = Mathf.Clamp(Mathf.RoundToInt(u * (brushState.TextureWidth - 1)), 0, brushState.TextureWidth - 1);
            int sourceY = Mathf.Clamp(Mathf.RoundToInt(v * (brushState.TextureHeight - 1)), 0, brushState.TextureHeight - 1);
            return TintPixel(brushState.TexturePixels[(sourceY * brushState.TextureWidth) + sourceX], brushState.Tint);
        }

        private Color32 SampleFallbackBrush(
            BrushRuntimeState brushState,
            int x,
            int y,
            int centerX,
            int centerY,
            int radiusPixelsX,
            int radiusPixelsY)
        {
            float dx = radiusPixelsX <= 0 ? 0f : (float)(x - centerX) / radiusPixelsX;
            float dy = radiusPixelsY <= 0 ? 0f : (float)(y - centerY) / radiusPixelsY;

            if ((dx * dx) + (dy * dy) > 1f)
            {
                return new Color32(0, 0, 0, 0);
            }

            return TintPixel(new Color32(255, 255, 255, 255), brushState.Tint);
        }

        private void ResetPixelsToSurface()
        {
            Color32 surface = surfaceColor;
            Color32 transparent = new Color32(0, 0, 0, 0);

            for (int y = 0; y < textureHeight; y++)
            {
                for (int x = 0; x < textureWidth; x++)
                {
                    int index = (y * textureWidth) + x;
                    pixels[index] = spreadMask.IsCoverable(x, y) ? surface : transparent;
                }
            }
        }

        private bool TryWorldToNormalized(Vector3 worldPoint, out float normalizedX, out float normalizedY)
        {
            normalizedX = 0f;
            normalizedY = 0f;

            if (spriteRendererComponent == null || spriteRendererComponent.sprite == null)
            {
                return false;
            }

            Bounds localBounds = spriteRendererComponent.sprite.bounds;
            Vector3 localPoint = transform.InverseTransformPoint(worldPoint);

            if (Mathf.Approximately(localBounds.size.x, 0f) || Mathf.Approximately(localBounds.size.y, 0f))
            {
                return false;
            }

            normalizedX = (localPoint.x - localBounds.min.x) / localBounds.size.x;
            normalizedY = (localPoint.y - localBounds.min.y) / localBounds.size.y;
            return true;
        }

        private void GetNormalizedScatterOffset(Vector3 worldPoint, float scatterRadius, out float normalizedX, out float normalizedY)
        {
            float angle = NextRandom01() * Mathf.PI * 2f;
            float distance = Mathf.Sqrt(NextRandom01()) * scatterRadius;
            Vector3 offsetWorldPoint = worldPoint + new Vector3(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance, 0f);

            if (!TryWorldToNormalized(offsetWorldPoint, out normalizedX, out normalizedY)
                || !TryWorldToNormalized(worldPoint, out float originX, out float originY))
            {
                normalizedX = 0f;
                normalizedY = 0f;
                return;
            }

            normalizedX -= originX;
            normalizedY -= originY;
        }

        private void GetNormalizedRadius(float radiusWorldUnits, out float normalizedRadiusX, out float normalizedRadiusY)
        {
            Bounds worldBounds = spriteRendererComponent.bounds;
            normalizedRadiusX = worldBounds.size.x <= 0f ? 0f : radiusWorldUnits / worldBounds.size.x;
            normalizedRadiusY = worldBounds.size.y <= 0f ? 0f : radiusWorldUnits / worldBounds.size.y;
        }

        private BrushRuntimeState[] BuildBrushRuntimeStates()
        {
            var states = new BrushRuntimeState[BrushCount];

            for (int i = 0; i < states.Length; i++)
            {
                states[i] = BrushRuntimeState.Create(brushes[i]);
            }

            return states;
        }

        private void EnsureBrushDefinitions()
        {
            if (brushes == null)
            {
                brushes = new List<SpreadBrushDefinition>();
            }

            if (brushes.Count > 0)
            {
                for (int i = 0; i < brushes.Count; i++)
                {
                    if (brushes[i] == null)
                    {
                        brushes[i] = CreateDefaultBrush(i);
                    }
                }

                return;
            }

            brushes.Add(CreateDefaultBrush(0));
            brushes.Add(CreateDefaultBrush(1));
        }

        private void NormalizeBrushDefinitions()
        {
            for (int i = 0; i < brushes.Count; i++)
            {
                brushes[i].NormalizeValues();
            }
        }

        private static SpreadBrushDefinition CreateDefaultBrush(int index)
        {
            return index == 1
                ? new SpreadBrushDefinition("seasoned-rice", "Seasoned Rice", null, new Color(0.98f, 0.86f, 0.62f, 1f))
                : new SpreadBrushDefinition("white-rice", "White Rice", null, new Color(1f, 0.97f, 0.86f, 1f));
        }

        private bool IsValidBrushIndex(int brushIndex)
        {
            return brushIndex >= 0 && brushIndex < BrushCount;
        }

        private void ApplyTexture()
        {
            spreadTexture.SetPixels32(pixels);
            spreadTexture.Apply(false);
        }

        private float NextRandom01()
        {
            return (float)random.NextDouble();
        }

        private void DestroyGeneratedAssets()
        {
            if (generatedSprite != null)
            {
                DestroyAsset(generatedSprite);
                generatedSprite = null;
            }

            if (spreadTexture != null)
            {
                DestroyAsset(spreadTexture);
                spreadTexture = null;
            }
        }

        private static Color32 TintPixel(Color32 source, Color tint)
        {
            return new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(source.r * tint.r), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(source.g * tint.g), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(source.b * tint.b), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(source.a * tint.a), 0, 255));
        }

        private static Color32 AlphaBlend(Color32 destination, Color32 source)
        {
            float sourceAlpha = source.a / 255f;
            if (sourceAlpha <= 0f)
            {
                return destination;
            }

            float destinationAlpha = destination.a / 255f;
            float inverseSourceAlpha = 1f - sourceAlpha;
            float outputAlpha = sourceAlpha + (destinationAlpha * inverseSourceAlpha);

            if (outputAlpha <= 0f)
            {
                return new Color32(0, 0, 0, 0);
            }

            return new Color32(
                BlendChannel(destination.r, destinationAlpha, source.r, sourceAlpha, inverseSourceAlpha, outputAlpha),
                BlendChannel(destination.g, destinationAlpha, source.g, sourceAlpha, inverseSourceAlpha, outputAlpha),
                BlendChannel(destination.b, destinationAlpha, source.b, sourceAlpha, inverseSourceAlpha, outputAlpha),
                (byte)Mathf.Clamp(Mathf.RoundToInt(outputAlpha * 255f), 0, 255));
        }

        private static byte BlendChannel(
            byte destination,
            float destinationAlpha,
            byte source,
            float sourceAlpha,
            float inverseSourceAlpha,
            float outputAlpha)
        {
            float blended = ((source * sourceAlpha) + (destination * destinationAlpha * inverseSourceAlpha)) / outputAlpha;
            return (byte)Mathf.Clamp(Mathf.RoundToInt(blended), 0, 255);
        }

        private static void DestroyAsset(UnityEngine.Object asset)
        {
            if (Application.isPlaying)
            {
                Destroy(asset);
            }
            else
            {
                DestroyImmediate(asset);
            }
        }

        private sealed class BrushRuntimeState
        {
            public Color Tint { get; private set; }

            public bool HasReadableTexture { get; private set; }

            public Color32[] TexturePixels { get; private set; }

            public int TextureWidth { get; private set; }

            public int TextureHeight { get; private set; }

            public static BrushRuntimeState Create(SpreadBrushDefinition definition)
            {
                var state = new BrushRuntimeState
                {
                    Tint = definition.Tint,
                    TexturePixels = Array.Empty<Color32>(),
                    TextureWidth = 0,
                    TextureHeight = 0,
                    HasReadableTexture = false
                };

                Texture2D texture = definition.BrushTexture;
                if (texture == null)
                {
                    return state;
                }

                try
                {
                    state.TexturePixels = texture.GetPixels32();
                    state.TextureWidth = texture.width;
                    state.TextureHeight = texture.height;
                    state.HasReadableTexture = state.TexturePixels.Length > 0 && state.TextureWidth > 0 && state.TextureHeight > 0;
                }
                catch (UnityException)
                {
                    state.TexturePixels = Array.Empty<Color32>();
                    state.TextureWidth = 0;
                    state.TextureHeight = 0;
                    state.HasReadableTexture = false;
                }

                return state;
            }
        }
    }
}
