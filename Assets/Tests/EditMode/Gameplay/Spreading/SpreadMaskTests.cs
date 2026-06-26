using NUnit.Framework;
using GameJam.Gameplay.Spreading;

namespace GameJam.Gameplay.Spreading.Tests
{
    public sealed class SpreadMaskTests
    {
        [Test]
        public void NewMask_StartsWithZeroCoverage()
        {
            var mask = new SpreadMask(32, 32, SpreadSurfaceShape.Ellipse);

            Assert.That(mask.CoverablePixelCount, Is.GreaterThan(0));
            Assert.That(mask.PaintedPixelCount, Is.EqualTo(0));
            Assert.That(mask.Coverage, Is.EqualTo(0f));
        }

        [Test]
        public void PaintNormalized_WhenPointIsCentered_IncreasesCoverage()
        {
            var mask = new SpreadMask(32, 32, SpreadSurfaceShape.Ellipse);

            int changedPixels = mask.PaintNormalized(0.5f, 0.5f, 0.1f, 0.1f);

            Assert.That(changedPixels, Is.GreaterThan(0));
            Assert.That(mask.Coverage, Is.GreaterThan(0f));
        }

        [Test]
        public void PaintNormalized_WhenPointIsOutsideSurface_DoesNotChangeCoverage()
        {
            var mask = new SpreadMask(32, 32, SpreadSurfaceShape.Ellipse);

            int changedPixels = mask.PaintNormalized(1.25f, 0.5f, 0.1f, 0.1f);

            Assert.That(changedPixels, Is.EqualTo(0));
            Assert.That(mask.Coverage, Is.EqualTo(0f));
        }

        [Test]
        public void PaintNormalized_WhenSameAreaIsPaintedTwice_DoesNotDoubleCount()
        {
            var mask = new SpreadMask(32, 32, SpreadSurfaceShape.Ellipse);

            int firstChange = mask.PaintNormalized(0.5f, 0.5f, 0.15f, 0.15f);
            float coverageAfterFirstPaint = mask.Coverage;
            int secondChange = mask.PaintNormalized(0.5f, 0.5f, 0.15f, 0.15f);

            Assert.That(firstChange, Is.GreaterThan(0));
            Assert.That(secondChange, Is.EqualTo(0));
            Assert.That(mask.Coverage, Is.EqualTo(coverageAfterFirstPaint));
        }

        [Test]
        public void PaintPixel_WhenUsedForDifferentBrushesAtSamePosition_SharesCoverage()
        {
            var mask = new SpreadMask(32, 32, SpreadSurfaceShape.Rectangle);

            bool firstBrushChangedCoverage = mask.PaintPixel(16, 16);
            float coverageAfterFirstBrush = mask.Coverage;
            bool secondBrushChangedCoverage = mask.PaintPixel(16, 16);

            Assert.That(firstBrushChangedCoverage, Is.True);
            Assert.That(secondBrushChangedCoverage, Is.False);
            Assert.That(mask.Coverage, Is.EqualTo(coverageAfterFirstBrush));
        }

        [Test]
        public void PaintPixel_WhenUsedForDifferentBrushesAtDifferentPositions_IncreasesSharedCoverage()
        {
            var mask = new SpreadMask(32, 32, SpreadSurfaceShape.Rectangle);

            mask.PaintPixel(8, 8);
            float coverageAfterFirstBrush = mask.Coverage;
            mask.PaintPixel(24, 24);

            Assert.That(mask.Coverage, Is.GreaterThan(coverageAfterFirstBrush));
        }

        [Test]
        public void Reset_AfterPainting_ReturnsCoverageToZero()
        {
            var mask = new SpreadMask(32, 32, SpreadSurfaceShape.Ellipse);
            mask.PaintNormalized(0.5f, 0.5f, 0.15f, 0.15f);

            mask.Reset();

            Assert.That(mask.PaintedPixelCount, Is.EqualTo(0));
            Assert.That(mask.Coverage, Is.EqualTo(0f));
        }

        [Test]
        public void IsComplete_WhenCoverageMeetsRequiredThreshold_ReturnsTrue()
        {
            var mask = new SpreadMask(16, 16, SpreadSurfaceShape.Rectangle);
            mask.PaintNormalized(0.5f, 0.5f, 1f, 1f);

            Assert.That(mask.Coverage, Is.GreaterThanOrEqualTo(0.7f));
            Assert.That(mask.IsComplete(0.7f), Is.True);
        }
    }
}
