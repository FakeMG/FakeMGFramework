using FakeMG.Framework.ExtensionMethods;
using NUnit.Framework;

namespace ExtensionMethods.EditMode
{
    [TestFixture]
    public class NumberShortHandExtensionsTests
    {
        [TestCase(0, 1, "0")]
        [TestCase(999, 1, "999")]
        [TestCase(1000, 1, "1K")]
        [TestCase(1500, 1, "1.5K")]
        [TestCase(1000000, 1, "1M")]
        [TestCase(2500000, 1, "2.5M")]
        [TestCase(1000000000, 1, "1B")]
        [TestCase(1234567890, 2, "1.23B")]
        [TestCase(-1500, 1, "-1.5K")]
        public void ToShorthand_Int_Works(int value, int decimalPlaces, string expected)
        {
            var result = value.ToShorthand(decimalPlaces);
            Assert.AreEqual(expected, result);
        }

        [TestCase(0L, 1, "0")]
        [TestCase(999L, 1, "999")]
        [TestCase(1000L, 1, "1K")]
        [TestCase(1500L, 1, "1.5K")]
        [TestCase(1000000L, 1, "1M")]
        [TestCase(2500000L, 1, "2.5M")]
        [TestCase(1000000000L, 1, "1B")]
        [TestCase(1234567890L, 2, "1.23B")]
        [TestCase(1000000000000L, 1, "1T")]
        [TestCase(-1500L, 1, "-1.5K")]
        public void ToShorthand_Long_Works(long value, int decimalPlaces, string expected)
        {
            var result = value.ToShorthand(decimalPlaces);
            Assert.AreEqual(expected, result);
        }

        [TestCase(0.5f, 1, "0.5")]
        [TestCase(999.1f, 1, "999.1")]
        [TestCase(1000.2f, 1, "1K")]
        [TestCase(1500.3f, 1, "1.5K")]
        [TestCase(1000000.4f, 1, "1M")]
        [TestCase(2500000.6f, 1, "2.5M")]
        [TestCase(1000000000.9f, 1, "1B")]
        [TestCase(1234567890f, 2, "1.23B")]
        [TestCase(-1500.7f, 1, "-1.5K")]
        public void ToShorthand_Float_Works(float value, int decimalPlaces, string expected)
        {
            var result = value.ToShorthand(decimalPlaces);
            Assert.AreEqual(expected, result);
        }

        [TestCase(0.56513d, 1, "0.6")]
        [TestCase(999.1d, 1, "999.1")]
        [TestCase(1000.23456d, 1, "1K")]
        [TestCase(1500.3785d, 1, "1.5K")]
        [TestCase(1000000.45656d, 1, "1M")]
        [TestCase(2500000.63789d, 1, "2.5M")]
        [TestCase(1000000000.95354d, 1, "1B")]
        [TestCase(1234567890d, 2, "1.23B")]
        [TestCase(1000000000000d, 1, "1T")]
        [TestCase(-1500.71651d, 1, "-1.5K")]
        public void ToShorthand_Double_Works(double value, int decimalPlaces, string expected)
        {
            var result = value.ToShorthand(decimalPlaces);
            Assert.AreEqual(expected, result);
        }
    }
}