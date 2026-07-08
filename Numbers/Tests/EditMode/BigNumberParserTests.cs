using System.Numerics;
using FakeMG.Numbers;
using NUnit.Framework;

namespace Numbers.EditMode
{
    [TestFixture]
    public class BigNumberParserTests
    {
        #region TryParse

        [TestCase("123", "123")]
        [TestCase("1k", "1000")]
        [TestCase("1K", "1000")]
        [TestCase("1.5K", "1500")]
        [TestCase("2.5M", "2500000")]
        [TestCase("1B", "1000000000")]
        [TestCase("1T", "1000000000000")]
        [TestCase("1QD", "1000000000000000")]
        [TestCase("1QN", "1000000000000000000")]
        [TestCase("1SX", "1000000000000000000000")]
        [TestCase("1SP", "1000000000000000000000000")]
        [TestCase("1,000", "1000")]
        [TestCase(" 1K ", "1000")]
        public void TryParse_ValidText_ReturnsExpectedValue(string text, string expectedDecimal)
        {
            bool result = BigNumberParser.TryParse(text, out BigInteger value);

            Assert.IsTrue(result, "TryParse should succeed for valid text");
            Assert.AreEqual(BigInteger.Parse(expectedDecimal), value);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("-5")]
        [TestCase("5X")]
        [TestCase("1.2.3")]
        [TestCase("abc")]
        [TestCase("0")] // A parsed amount of exactly zero is treated as "nothing entered" and rejected.
        public void TryParse_InvalidText_ReturnsFalseAndZero(string text)
        {
            bool result = BigNumberParser.TryParse(text, out BigInteger value);

            Assert.IsFalse(result, "TryParse should fail for invalid text");
            Assert.AreEqual(BigInteger.Zero, value);
        }

        #endregion

        #region ParseOrDefault

        [Test]
        public void ParseOrDefault_ValidText_ReturnsParsedValue()
        {
            BigInteger result = BigNumberParser.ParseOrDefault("1K", BigInteger.MinusOne);

            Assert.AreEqual(new BigInteger(1000), result);
        }

        [Test]
        public void ParseOrDefault_InvalidText_ReturnsFallback()
        {
            var fallback = new BigInteger(42);

            BigInteger result = BigNumberParser.ParseOrDefault("not a number", fallback);

            Assert.AreEqual(fallback, result);
        }

        #endregion

        #region TryParseDouble

        [TestCase("1.5", 1.5d)]
        [TestCase("1K", 1000d)]
        [TestCase("1.5K", 1500d)]
        [TestCase("0", 0d)]
        public void TryParseDouble_ValidText_ReturnsExpectedValue(string text, double expected)
        {
            bool result = BigNumberParser.TryParseDouble(text, out double value);

            Assert.IsTrue(result, "TryParseDouble should succeed for valid text");
            Assert.AreEqual(expected, value, 0.0001d);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("-1")]
        [TestCase("abc")]
        public void TryParseDouble_InvalidText_ReturnsFalseAndZero(string text)
        {
            bool result = BigNumberParser.TryParseDouble(text, out double value);

            Assert.IsFalse(result, "TryParseDouble should fail for invalid text");
            Assert.AreEqual(0d, value);
        }

        [Test]
        public void ParseDoubleOrDefault_InvalidText_ReturnsFallback()
        {
            const double FALLBACK = 3.14d;

            double result = BigNumberParser.ParseDoubleOrDefault("not a number", FALLBACK);

            Assert.AreEqual(FALLBACK, result);
        }

        #endregion

        #region ToShorthand

        [TestCase(0, 1, "0")]
        [TestCase(999, 1, "999")]
        [TestCase(1000, 1, "1K")]
        [TestCase(999, 1, "999")]
        [TestCase(999999, 1, "999.9K")]
        [TestCase(1000000, 1, "1M")]
        [TestCase(1500, 1, "1.5K")]
        [TestCase(1200, 1, "1.2K")]
        [TestCase(1234567890, 2, "1.23B")]
        [TestCase(-1500, 1, "-1.5K")]
        [TestCase(1500, 0, "1K")]
        public void ToShorthand_Int64Range_ReturnsExpectedText(long number, int decimalPlaces, string expected)
        {
            string result = BigNumberParser.ToShorthand(new BigInteger(number), decimalPlaces);

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void ToShorthand_Trillion_ReturnsExpectedText()
        {
            string result = BigNumberParser.ToShorthand(BigInteger.Pow(10, 12), 1);

            Assert.AreEqual("1T", result);
        }

        [Test]
        public void ToShorthand_Quadrillion_ReturnsExpectedText()
        {
            string result = BigNumberParser.ToShorthand(BigInteger.Pow(10, 15), 1);

            Assert.AreEqual("1Qd", result);
        }

        [Test]
        public void ToShorthand_Quintillion_ReturnsExpectedText()
        {
            string result = BigNumberParser.ToShorthand(BigInteger.Pow(10, 18), 1);

            Assert.AreEqual("1Qn", result);
        }

        [Test]
        public void ToShorthand_Sextillion_ReturnsExpectedText()
        {
            string result = BigNumberParser.ToShorthand(BigInteger.Pow(10, 21), 1);

            Assert.AreEqual("1Sx", result);
        }

        [Test]
        public void ToShorthand_Septillion_ReturnsExpectedText()
        {
            string result = BigNumberParser.ToShorthand(BigInteger.Pow(10, 24), 1);

            Assert.AreEqual("1Sp", result);
        }

        #endregion
    }
}
