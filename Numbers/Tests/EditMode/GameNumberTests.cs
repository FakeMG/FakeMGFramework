using FakeMG.Numbers;
using NUnit.Framework;

namespace Numbers.EditMode
{
    [TestFixture]
    public class GameNumberTests
    {
        #region Construction & Conversion

        [Test]
        public void Zero_IsZero()
        {
            Assert.AreEqual(0, (int)GameNumber.Zero);
        }

        [Test]
        public void One_IsOne()
        {
            Assert.AreEqual(1, (int)GameNumber.One);
        }

        [Test]
        public void ImplicitConversion_FromInt_PreservesValue()
        {
            GameNumber number = 42;

            Assert.AreEqual(42, (int)number);
        }

        [Test]
        public void ImplicitConversion_FromLong_PreservesValue()
        {
            GameNumber number = 42L;

            Assert.AreEqual(42, (int)number);
        }

        [Test]
        public void FromDouble_ToDouble_RoundTrips()
        {
            GameNumber number = GameNumber.FromDouble(123d);

            Assert.AreEqual(123d, number.ToDouble());
        }

        #endregion

        #region Arithmetic Operators

        [Test]
        public void Addition_ReturnsSum()
        {
            GameNumber result = (GameNumber)10 + (GameNumber)5;

            Assert.AreEqual(15, (int)result);
        }

        [Test]
        public void Subtraction_ReturnsDifference()
        {
            GameNumber result = (GameNumber)10 - (GameNumber)5;

            Assert.AreEqual(5, (int)result);
        }

        [Test]
        public void Multiplication_ReturnsProduct()
        {
            GameNumber result = (GameNumber)10 * (GameNumber)5;

            Assert.AreEqual(50, (int)result);
        }

        #endregion

        #region Comparison Operators

        [Test]
        public void Equality_SameValue_ReturnsTrue()
        {
            Assert.IsTrue((GameNumber)5 == (GameNumber)5);
            Assert.IsFalse((GameNumber)5 != (GameNumber)5);
        }

        [Test]
        public void Equality_DifferentValue_ReturnsFalse()
        {
            Assert.IsFalse((GameNumber)5 == (GameNumber)6);
            Assert.IsTrue((GameNumber)5 != (GameNumber)6);
        }

        [Test]
        public void LessThan_SmallerValue_ReturnsTrue()
        {
            Assert.IsTrue((GameNumber)5 < (GameNumber)10);
            Assert.IsFalse((GameNumber)10 < (GameNumber)5);
        }

        [Test]
        public void LessThanOrEqual_EqualValue_ReturnsTrue()
        {
            Assert.IsTrue((GameNumber)5 <= (GameNumber)5);
        }

        [Test]
        public void GreaterThan_LargerValue_ReturnsTrue()
        {
            Assert.IsTrue((GameNumber)10 > (GameNumber)5);
            Assert.IsFalse((GameNumber)5 > (GameNumber)10);
        }

        [Test]
        public void GreaterThanOrEqual_EqualValue_ReturnsTrue()
        {
            Assert.IsTrue((GameNumber)5 >= (GameNumber)5);
        }

        [Test]
        public void Max_ReturnsLargerValueRegardlessOfArgumentOrder()
        {
            Assert.AreEqual(10, (int)GameNumber.Max((GameNumber)10, (GameNumber)5));
            Assert.AreEqual(10, (int)GameNumber.Max((GameNumber)5, (GameNumber)10));
        }

        [Test]
        public void Max_EqualValues_ReturnsThatValue()
        {
            Assert.AreEqual(5, (int)GameNumber.Max((GameNumber)5, (GameNumber)5));
        }

        #endregion

        #region Pow

        [Test]
        public void Pow_ExponentZero_ReturnsOne()
        {
            GameNumber result = GameNumber.Pow((GameNumber)7, 0);

            Assert.AreEqual(1, (int)result);
        }

        [Test]
        public void Pow_ExponentOne_ReturnsBaseValue()
        {
            GameNumber result = GameNumber.Pow((GameNumber)7, 1);

            Assert.AreEqual(7, (int)result);
        }

        [Test]
        public void Pow_ExponentGreaterThanOne_ReturnsExpectedPower()
        {
            GameNumber result = GameNumber.Pow((GameNumber)2, 10);

            Assert.AreEqual(1024, (int)result);
        }

        #endregion

        #region Formatting

        [Test]
        public void ToString_ReturnsInvariantDecimalRepresentation()
        {
            GameNumber number = (GameNumber)1234;

            Assert.AreEqual("1234", number.ToString());
        }

        [Test]
        public void SeparateNumberWithComma_InsertsThousandsSeparators()
        {
            GameNumber number = (GameNumber)1234567;

            Assert.AreEqual("1,234,567", number.SeparateNumberWithComma());
        }

        #endregion

        #region TryParse

        [Test]
        public void TryParse_ValidText_ReturnsTrueAndParsedValue()
        {
            bool result = GameNumber.TryParse("1.5K", out GameNumber value);

            Assert.IsTrue(result);
            Assert.AreEqual(1500, (int)value);
        }

        [Test]
        public void TryParse_InvalidText_ReturnsFalseAndZero()
        {
            bool result = GameNumber.TryParse("not a number", out GameNumber value);

            Assert.IsFalse(result);
            Assert.AreEqual(GameNumber.Zero, value);
        }

        [Test]
        public void ParseOrDefault_ValidText_ReturnsParsedValue()
        {
            GameNumber result = GameNumber.ParseOrDefault("1K", GameNumber.Zero);

            Assert.AreEqual(1000, (int)result);
        }

        [Test]
        public void ParseOrDefault_InvalidText_ReturnsFallback()
        {
            GameNumber fallback = (GameNumber)99;

            GameNumber result = GameNumber.ParseOrDefault("not a number", fallback);

            Assert.AreEqual(fallback, result);
        }

        #endregion

        #region Equality & Ordering Contracts

        [Test]
        public void Equals_SameValue_ReturnsTrueAndMatchingHashCode()
        {
            GameNumber a = (GameNumber)5;
            GameNumber b = (GameNumber)5;

            Assert.IsTrue(a.Equals(b));
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        public void Equals_Object_NonGameNumber_ReturnsFalse()
        {
            GameNumber number = (GameNumber)5;

            Assert.IsFalse(number.Equals("5"));
        }

        [Test]
        public void CompareTo_SmallerValue_ReturnsNegative()
        {
            GameNumber smaller = (GameNumber)5;
            GameNumber larger = (GameNumber)10;

            Assert.Less(smaller.CompareTo(larger), 0);
        }

        [Test]
        public void CompareTo_LargerValue_ReturnsPositive()
        {
            GameNumber smaller = (GameNumber)5;
            GameNumber larger = (GameNumber)10;

            Assert.Greater(larger.CompareTo(smaller), 0);
        }

        [Test]
        public void CompareTo_EqualValue_ReturnsZero()
        {
            GameNumber a = (GameNumber)5;
            GameNumber b = (GameNumber)5;

            Assert.AreEqual(0, a.CompareTo(b));
        }

        #endregion
    }
}
