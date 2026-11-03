using NUnit.Framework;
using UnityEngine;

namespace FakeMG.Outline.Tests
{
    public sealed class OutlineScreenBoundsTests
    {
        #region Public Methods

        [Test]
        public void CenteredSelection_ProducesPaddedPixelBounds()
        {
            RectInt rectanglePixels = OutlineScreenBounds.CalculatePixelRect(new Bounds(Vector3.zero, Vector3.one),
                Matrix4x4.identity, 100, 100, 10, false);

            Assert.That(rectanglePixels, Is.EqualTo(new RectInt(15, 15, 70, 70)));
        }

        [Test]
        public void OffscreenSelection_ProducesNoWork()
        {
            RectInt rectanglePixels = OutlineScreenBounds.CalculatePixelRect(new Bounds(new Vector3(4f, 0f, 0f), Vector3.one),
                Matrix4x4.identity, 100, 100, 10, false);

            Assert.That(rectanglePixels.width, Is.Zero);
        }

        [Test]
        public void NearPlaneIntersection_FallsBackToFullViewport()
        {
            RectInt rectanglePixels = OutlineScreenBounds.CalculatePixelRect(new Bounds(Vector3.zero, Vector3.one * 3f),
                Matrix4x4.identity, 100, 80, 10, false);

            Assert.That(rectanglePixels, Is.EqualTo(new RectInt(0, 0, 100, 80)));
        }

        [Test]
        public void InvalidTargetSize_ProducesNoWork()
        {
            RectInt rectanglePixels = OutlineScreenBounds.CalculatePixelRect(new Bounds(Vector3.zero, Vector3.one),
                Matrix4x4.identity, 0, 100, 10, false);

            Assert.That(rectanglePixels, Is.EqualTo(default(RectInt)));
        }

        [Test]
        public void TopOrigin_ReflectsAsymmetricBoundsVertically()
        {
            Bounds boundsMeters = new(new Vector3(0f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0.5f));
            RectInt bottomOriginPixels = OutlineScreenBounds.CalculatePixelRect(boundsMeters, Matrix4x4.identity, 100, 100, 0, false);
            RectInt topOriginPixels = OutlineScreenBounds.CalculatePixelRect(boundsMeters, Matrix4x4.identity, 100, 100, 0, true);

            Assert.That(topOriginPixels.yMin, Is.EqualTo(100 - bottomOriginPixels.yMax));
            Assert.That(topOriginPixels.size, Is.EqualTo(bottomOriginPixels.size));
        }

        [Test]
        public void SmallSelection_ReducesProcessingAreaWithoutReducingResolution()
        {
            RectInt rectanglePixels = OutlineScreenBounds.CalculatePixelRect(new Bounds(Vector3.zero, new Vector3(0.2f, 0.2f, 0.2f)),
                Matrix4x4.identity, 1920, 1080, 12, false);

            Assert.That(rectanglePixels.width * rectanglePixels.height, Is.LessThan(1920 * 1080 / 50));
        }

        [Test]
        public void AllocationAlignment_PreservesCoverageAndStaysWithinTheViewport()
        {
            Bounds boundsMeters = new(new Vector3(0.8f, 0.8f, 0f), new Vector3(0.3f, 0.3f, 0.3f));
            RectInt exactPixels = OutlineScreenBounds.CalculatePixelRect(boundsMeters, Matrix4x4.identity, 100, 100, 4, false);
            RectInt alignedPixels = OutlineScreenBounds.CalculatePixelRect(boundsMeters, Matrix4x4.identity, 100, 100, 4, false, 32);

            Assert.That(alignedPixels.width % 32, Is.Zero);
            Assert.That(alignedPixels.height % 32, Is.Zero);
            Assert.That(alignedPixels.xMin, Is.LessThanOrEqualTo(exactPixels.xMin));
            Assert.That(alignedPixels.yMin, Is.LessThanOrEqualTo(exactPixels.yMin));
            Assert.That(alignedPixels.xMax, Is.GreaterThanOrEqualTo(exactPixels.xMax).And.LessThanOrEqualTo(100));
            Assert.That(alignedPixels.yMax, Is.GreaterThanOrEqualTo(exactPixels.yMax).And.LessThanOrEqualTo(100));
        }

        #endregion
    }
}
