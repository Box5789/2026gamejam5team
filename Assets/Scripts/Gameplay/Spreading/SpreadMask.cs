using System;

namespace GameJam.Gameplay.Spreading
{
    public sealed class SpreadMask
    {
        private const float Epsilon = 0.0001f;

        private readonly bool[] coverablePixels;
        private readonly bool[] paintedPixels;

        public SpreadMask(int width, int height, SpreadSurfaceShape shape)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be greater than zero.");
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be greater than zero.");
            }

            Width = width;
            Height = height;
            Shape = shape;
            coverablePixels = new bool[width * height];
            paintedPixels = new bool[width * height];

            InitializeCoverablePixels();
        }

        public int Width { get; }

        public int Height { get; }

        public SpreadSurfaceShape Shape { get; }

        public int CoverablePixelCount { get; private set; }

        public int PaintedPixelCount { get; private set; }

        public float Coverage => CoverablePixelCount == 0 ? 0f : (float)PaintedPixelCount / CoverablePixelCount;

        public bool IsComplete(float requiredCoverage)
        {
            return Coverage + Epsilon >= Clamp01(requiredCoverage);
        }

        public bool IsCoverable(int x, int y)
        {
            return IsInPixelBounds(x, y) && coverablePixels[GetIndex(x, y)];
        }

        public bool IsPainted(int x, int y)
        {
            return IsInPixelBounds(x, y) && paintedPixels[GetIndex(x, y)];
        }

        public bool PaintPixel(int x, int y)
        {
            if (!IsInPixelBounds(x, y))
            {
                return false;
            }

            int index = GetIndex(x, y);
            if (!coverablePixels[index] || paintedPixels[index])
            {
                return false;
            }

            paintedPixels[index] = true;
            PaintedPixelCount++;
            return true;
        }

        public bool ContainsNormalizedPoint(float normalizedX, float normalizedY)
        {
            if (normalizedX < 0f || normalizedX > 1f || normalizedY < 0f || normalizedY > 1f)
            {
                return false;
            }

            if (Shape == SpreadSurfaceShape.Rectangle)
            {
                return true;
            }

            float dx = (normalizedX - 0.5f) * 2f;
            float dy = (normalizedY - 0.5f) * 2f;
            return (dx * dx) + (dy * dy) <= 1f + Epsilon;
        }

        public int PaintNormalized(float normalizedX, float normalizedY, float normalizedRadiusX, float normalizedRadiusY)
        {
            return PaintNormalized(normalizedX, normalizedY, normalizedRadiusX, normalizedRadiusY, true);
        }

        public int PaintNormalized(
            float normalizedX,
            float normalizedY,
            float normalizedRadiusX,
            float normalizedRadiusY,
            bool requireCenterInside)
        {
            if (normalizedRadiusX <= 0f || normalizedRadiusY <= 0f)
            {
                return 0;
            }

            if (requireCenterInside && !ContainsNormalizedPoint(normalizedX, normalizedY))
            {
                return 0;
            }

            int minX = Math.Max(0, NormalizedXToPixel(normalizedX - normalizedRadiusX));
            int maxX = Math.Min(Width - 1, NormalizedXToPixel(normalizedX + normalizedRadiusX));
            int minY = Math.Max(0, NormalizedYToPixel(normalizedY - normalizedRadiusY));
            int maxY = Math.Min(Height - 1, NormalizedYToPixel(normalizedY + normalizedRadiusY));
            int newlyPainted = 0;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = (GetPixelCenterX(x) - normalizedX) / normalizedRadiusX;
                    float dy = (GetPixelCenterY(y) - normalizedY) / normalizedRadiusY;

                    if ((dx * dx) + (dy * dy) > 1f + Epsilon)
                    {
                        continue;
                    }

                    if (PaintPixel(x, y))
                    {
                        newlyPainted++;
                    }
                }
            }

            return newlyPainted;
        }

        public void Reset()
        {
            Array.Clear(paintedPixels, 0, paintedPixels.Length);
            PaintedPixelCount = 0;
        }

        private void InitializeCoverablePixels()
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (!ContainsNormalizedPoint(GetPixelCenterX(x), GetPixelCenterY(y)))
                    {
                        continue;
                    }

                    coverablePixels[GetIndex(x, y)] = true;
                    CoverablePixelCount++;
                }
            }
        }

        private bool IsInPixelBounds(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        private int GetIndex(int x, int y)
        {
            return y * Width + x;
        }

        private int NormalizedXToPixel(float normalizedX)
        {
            return NormalizedToPixel(normalizedX, Width);
        }

        private int NormalizedYToPixel(float normalizedY)
        {
            return NormalizedToPixel(normalizedY, Height);
        }

        private static int NormalizedToPixel(float normalizedValue, int size)
        {
            float clamped = Math.Max(0f, Math.Min(1f, normalizedValue));
            return Math.Min(size - 1, (int)Math.Floor(clamped * size));
        }

        private float GetPixelCenterX(int x)
        {
            return (x + 0.5f) / Width;
        }

        private float GetPixelCenterY(int y)
        {
            return (y + 0.5f) / Height;
        }

        private static float Clamp01(float value)
        {
            return Math.Max(0f, Math.Min(1f, value));
        }
    }
}
