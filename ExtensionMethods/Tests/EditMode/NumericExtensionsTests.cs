using FakeMG.Framework.ExtensionMethods;
using NUnit.Framework;

namespace ExtensionMethods.EditMode
{
    public class NumericExtensionsTests
    {
        private const float FLOAT_TOLERANCE = 0.0001f;

        #region Remap (float)

        [Test]
        public void Remap_MidpointOfInputRange_ReturnsMidpointOfOutputRange()
        {
            Assert.AreEqual(5f, 0.5f.Remap(0f, 1f, 0f, 10f), FLOAT_TOLERANCE);
        }

        [Test]
        public void Remap_ValueAtFromMin_ReturnsToMin()
        {
            Assert.AreEqual(0f, 0f.Remap(0f, 1f, 0f, 10f), FLOAT_TOLERANCE);
        }

        [Test]
        public void Remap_ValueAtFromMax_ReturnsToMax()
        {
            Assert.AreEqual(10f, 1f.Remap(0f, 1f, 0f, 10f), FLOAT_TOLERANCE);
        }

        [Test]
        public void Remap_ValueOutsideInputRange_ExtrapolatesLinearly()
        {
            Assert.AreEqual(20f, 2f.Remap(0f, 1f, 0f, 10f), FLOAT_TOLERANCE);
            Assert.AreEqual(-10f, (-1f).Remap(0f, 1f, 0f, 10f), FLOAT_TOLERANCE);
        }

        [Test]
        public void Remap_InvertedOutputRange_MapsCorrectly()
        {
            Assert.AreEqual(7.5f, 0.25f.Remap(0f, 1f, 10f, 0f), FLOAT_TOLERANCE);
        }

        #endregion

        #region Remap (int)

        [Test]
        public void Remap_Int_PositiveResult_TruncatesTowardZero()
        {
            // 59 in [0,100] -> [0,10] = 5.9, truncates to 5
            Assert.AreEqual(5, 59.Remap(0, 100, 0, 10));
        }

        [Test]
        public void Remap_Int_NegativeResult_TruncatesTowardZero()
        {
            // 59 in [0,100] -> [0,-10] = -5.9, truncates to -5 (toward zero)
            Assert.AreEqual(-5, 59.Remap(0, 100, 0, -10));
        }

        #endregion

        #region Remap Edge Cases

        // A zero-width input range would divide by zero, so the guard returns toMin instead.
        [Test]
        public void Remap_ZeroWidthInputSpan_ReturnsToMin()
        {
            Assert.AreEqual(3f, 5f.Remap(1f, 1f, 3f, 10f), FLOAT_TOLERANCE);
        }

        #endregion
    }
}
