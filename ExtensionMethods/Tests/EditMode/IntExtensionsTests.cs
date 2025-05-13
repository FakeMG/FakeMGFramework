using NUnit.Framework;

namespace FakeMG.FakeMGFramework.ExtensionMethods.Tests {
    public class IntExtensionsTests {
        [Test]
        public void WrapValue_ValueWithinRange_ReturnsValue() {
            // Arrange & Act & Assert
            Assert.AreEqual(5, 5.WrapValue(0, 10));
            Assert.AreEqual(3, 3.WrapValue(1, 6));
            Assert.AreEqual(7, 7.WrapValue(-5, 10));
        }

        [Test]
        public void WrapValue_ValueEqualsMin_ReturnsMin() {
            // Arrange & Act & Assert
            Assert.AreEqual(0, 0.WrapValue(0, 10));
            Assert.AreEqual(5, 5.WrapValue(5, 10));
            Assert.AreEqual(-5, (-5).WrapValue(-5, 5));
        }

        [Test]
        public void WrapValue_ValueEqualsMax_ReturnsMax() {
            // Arrange & Act & Assert
            Assert.AreEqual(10, 10.WrapValue(0, 10));
            Assert.AreEqual(10, 10.WrapValue(5, 10));
            Assert.AreEqual(5, 5.WrapValue(-5, 5));
        }

        [Test]
        public void WrapValue_ValueGreaterThanMax_ReturnsWrappedValue() {
            // Arrange & Act & Assert
            Assert.AreEqual(10, 10.WrapValue(0, 10));
            Assert.AreEqual(0, 11.WrapValue(0, 10));
            Assert.AreEqual(9, 20.WrapValue(0, 10));
            Assert.AreEqual(3, 14.WrapValue(0, 10));
            Assert.AreEqual(-4, 7.WrapValue(-5, 5));
        }

        [Test]
        public void WrapValue_ValueLessThanMin_ReturnsWrappedValue() {
            // Arrange & Act & Assert
            Assert.AreEqual(10, (-1).WrapValue(0, 10));
            Assert.AreEqual(9, (-2).WrapValue(0, 10));
            Assert.AreEqual(1, (-10).WrapValue(0, 10));
            Assert.AreEqual(5, (-6).WrapValue(-5, 5));
        }

        [Test]
        public void WrapValue_NegativeRange_WorksCorrectly() {
            // Arrange & Act & Assert
            Assert.AreEqual(-10, (-10).WrapValue(-10, -5));
            Assert.AreEqual(-9, (-9).WrapValue(-10, -5));
            Assert.AreEqual(-5, (-5).WrapValue(-10, -5));
            Assert.AreEqual(-10, (-4).WrapValue(-10, -5));
            Assert.AreEqual(-9, (-15).WrapValue(-10, -5));
        }

        [Test]
        public void WrapValue_RangeWithZero_WorksCorrectly() {
            // Arrange & Act & Assert
            Assert.AreEqual(0, 0.WrapValue(-5, 5));
            Assert.AreEqual(-1, 10.WrapValue(-5, 5));
            Assert.AreEqual(0, 11.WrapValue(-5, 5));
            Assert.AreEqual(1, (-10).WrapValue(-5, 5));
            Assert.AreEqual(-5, (-5).WrapValue(-5, 5));
            Assert.AreEqual(5, 5.WrapValue(-5, 5));
        }

        [Test]
        public void WrapValue_LargeValues_WorksCorrectly() {
            // Arrange & Act & Assert
            Assert.AreEqual(5, 15.WrapValue(1, 10));
            Assert.AreEqual(10, (-1).WrapValue(0, 10));
            Assert.AreEqual(1, int.MaxValue.WrapValue(0, 10));
            Assert.AreEqual(9, int.MinValue.WrapValue(0, 10)); // Specific result depends on overflows
        }

        [Test]
        public void WrapValue_MinEqualsMax_ReturnsMin() {
            // Arrange & Act & Assert
            Assert.AreEqual(5, 10.WrapValue(5, 5)); // When min equals max, should always return min
            Assert.AreEqual(5, 5.WrapValue(5, 5));
            Assert.AreEqual(5, 0.WrapValue(5, 5));
            Assert.AreEqual(0, 100.WrapValue(0, 0));
        }
    }
}