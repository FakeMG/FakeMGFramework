using FakeMG.FakeMGFramework.ExtensionMethods;
using NUnit.Framework;

namespace ExtensionMethods.EditMode
{
    public class StringExtensionsTests
    {
        [Test]
        public void SeparateNumberWithComma_Int_SmallNumbers_ReturnsWithoutComma()
        {
            // Arrange & Act & Assert
            Assert.AreEqual("0", 0.SeparateNumberWithComma());
            Assert.AreEqual("1", 1.SeparateNumberWithComma());
            Assert.AreEqual("99", 99.SeparateNumberWithComma());
            Assert.AreEqual("999", 999.SeparateNumberWithComma());
        }

        [Test]
        public void SeparateNumberWithComma_Int_ThousandsRange_ReturnsWithComma()
        {
            // Arrange & Act & Assert
            Assert.AreEqual("1,000", 1000.SeparateNumberWithComma());
            Assert.AreEqual("1,234", 1234.SeparateNumberWithComma());
            Assert.AreEqual("12,345", 12345.SeparateNumberWithComma());
            Assert.AreEqual("999,999", 999999.SeparateNumberWithComma());
        }

        [Test]
        public void SeparateNumberWithComma_Int_MillionsRange_ReturnsWithCommas()
        {
            // Arrange & Act & Assert
            Assert.AreEqual("1,000,000", 1000000.SeparateNumberWithComma());
            Assert.AreEqual("1,234,567", 1234567.SeparateNumberWithComma());
            Assert.AreEqual("12,345,678", 12345678.SeparateNumberWithComma());
            Assert.AreEqual("123,456,789", 123456789.SeparateNumberWithComma());
        }

        [Test]
        public void SeparateNumberWithComma_Int_NegativeNumbers_ReturnsWithComma()
        {
            // Arrange & Act & Assert
            Assert.AreEqual("-1", (-1).SeparateNumberWithComma());
            Assert.AreEqual("-1,000", (-1000).SeparateNumberWithComma());
            Assert.AreEqual("-1,234,567", (-1234567).SeparateNumberWithComma());
        }

        [Test]
        public void SeparateNumberWithComma_Int_LargeNumbers_ReturnsWithCommas()
        {
            // Arrange & Act & Assert
            Assert.AreEqual("2,147,483,647", int.MaxValue.SeparateNumberWithComma());
            Assert.AreEqual("-2,147,483,648", int.MinValue.SeparateNumberWithComma());
        }

        [Test]
        public void SeparateNumberWithComma_Double_WholeNumbers_ReturnsWithComma()
        {
            // Arrange & Act & Assert
            Assert.AreEqual("0", 0.0.SeparateNumberWithComma());
            Assert.AreEqual("1", 1.0.SeparateNumberWithComma());
            Assert.AreEqual("1,000", 1000.0.SeparateNumberWithComma());
            Assert.AreEqual("1,234,567", 1234567.0.SeparateNumberWithComma());
        }

        [Test]
        public void SeparateNumberWithComma_Double_DecimalNumbers_RoundsAndSeparates()
        {
            // Arrange & Act & Assert
            Assert.AreEqual("1", 1.4.SeparateNumberWithComma());
            Assert.AreEqual("2", 1.5.SeparateNumberWithComma());
            Assert.AreEqual("1,235", 1234.6.SeparateNumberWithComma());
            Assert.AreEqual("1,234,568", 1234567.8.SeparateNumberWithComma());
        }

        [Test]
        public void SeparateNumberWithComma_Double_NegativeDecimals_RoundsAndSeparates()
        {
            // Arrange & Act & Assert
            Assert.AreEqual("-1", (-1.4).SeparateNumberWithComma());
            Assert.AreEqual("-2", (-1.5).SeparateNumberWithComma());
            Assert.AreEqual("-1,235", (-1234.6).SeparateNumberWithComma());
        }

        [Test]
        public void SeparateTextByUpperCase_CamelCase_ReturnsSeparatedWords()
        {
            // Arrange & Act & Assert
            Assert.AreEqual("Hello World", "helloWorld".SeparateTextByUpperCase());
            Assert.AreEqual("My Variable Name", "myVariableName".SeparateTextByUpperCase());
            Assert.AreEqual("Test Method", "testMethod".SeparateTextByUpperCase());
        }

        [Test]
        public void SeparateTextByUpperCase_PascalCase_ReturnsSeparatedWords()
        {
            // Arrange & Act & Assert
            Assert.AreEqual("Hello World", "HelloWorld".SeparateTextByUpperCase());
            Assert.AreEqual("My Variable Name", "MyVariableName".SeparateTextByUpperCase());
            Assert.AreEqual("Test Method", "TestMethod".SeparateTextByUpperCase());
        }

        [Test]
        public void SeparateTextByUpperCase_MultipleConsecutiveUpperCase_SeparatesEach()
        {
            // Arrange & Act & Assert
            Assert.AreEqual("HTML Parser", "HTMLParser".SeparateTextByUpperCase());
            Assert.AreEqual("XML Document", "XMLDocument".SeparateTextByUpperCase());
            Assert.AreEqual("URL Helper", "URLHelper".SeparateTextByUpperCase());
        }

        [Test]
        public void SeparateTextByUpperCase_MixedCaseWithNumbers_ReturnsSeparated()
        {
            // Arrange & Act & Assert
            Assert.AreEqual("Test123 Method", "test123Method".SeparateTextByUpperCase());
            Assert.AreEqual("My Variable2 Name", "myVariable2Name".SeparateTextByUpperCase());
        }

        [Test]
        public void SeparateTextByUpperCase_SingleCharacter_ReturnsCapitalized()
        {
            // Arrange & Act & Assert
            Assert.AreEqual("A", "a".SeparateTextByUpperCase());
            Assert.AreEqual("Z", "z".SeparateTextByUpperCase());
            Assert.AreEqual("X", "X".SeparateTextByUpperCase());
        }

        [Test]
        public void SeparateTextByUpperCase_ContainsOneCharacterWords_SeparatesThoseWords()
        {
            // Arrange & Act & Assert
            Assert.AreEqual("A Key", "aKey".SeparateTextByUpperCase());
            Assert.AreEqual("I Have A Key", "iHaveAKey".SeparateTextByUpperCase());
        }

        [Test]
        public void SeparateTextByUpperCase_AlreadyHasSpaces_PreservesAndSeparates()
        {
            // Arrange & Act & Assert
            Assert.AreEqual("Hello World Test", "hello WorldTest".SeparateTextByUpperCase());
            Assert.AreEqual("My Variable Name", "my VariableName".SeparateTextByUpperCase());
        }

        [Test]
        public void SeparateTextByUpperCase_AllUpperCase_SeparatesEachLetter()
        {
            // Arrange & Act & Assert
            Assert.AreEqual("HELLO", "HELLO".SeparateTextByUpperCase());
            Assert.AreEqual("TEST", "TEST".SeparateTextByUpperCase());
        }

        [Test]
        public void SeparateTextByUpperCase_AllLowerCase_ReturnsCapitalized()
        {
            // Arrange & Act & Assert
            Assert.AreEqual("Hello", "hello".SeparateTextByUpperCase());
            Assert.AreEqual("Testing method", "testing method".SeparateTextByUpperCase());
        }
    }
}