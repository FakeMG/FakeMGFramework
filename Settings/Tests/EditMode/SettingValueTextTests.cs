using NUnit.Framework;
using UnityEngine;

namespace FakeMG.Settings.Tests
{
    public sealed class SettingValueTextTests
    {
        [TestCase(0.25f)]
        [TestCase(-3.5f)]
        [TestCase(0f)]
        public void FormatThenParse_Float_RoundTrips(float value)
        {
            bool isParsed = SettingValueText.TryParse(SettingValueText.Format(value), out float parsedValue);

            Assert.That(isParsed, Is.True);
            Assert.That(parsedValue, Is.EqualTo(value));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void FormatThenParse_Bool_RoundTrips(bool value)
        {
            SettingValueText.TryParse(SettingValueText.Format(value), out bool parsedValue);

            Assert.That(parsedValue, Is.EqualTo(value));
        }

        [Test]
        public void FormatThenParse_Enum_RoundTripsByName()
        {
            string text = SettingValueText.Format(FullScreenMode.Windowed);
            SettingValueText.TryParse(text, out FullScreenMode parsedValue);

            Assert.That(text, Is.EqualTo(nameof(FullScreenMode.Windowed)));
            Assert.That(parsedValue, Is.EqualTo(FullScreenMode.Windowed));
        }

        [Test]
        public void Format_String_ReturnsItUnchanged()
        {
            Assert.That(SettingValueText.Format("1920x1080"), Is.EqualTo("1920x1080"));
        }

        [TestCase("abc")]
        [TestCase("")]
        [TestCase(null)]
        public void TryParse_InvalidInt_ReturnsFalse(string text)
        {
            Assert.That(SettingValueText.TryParse(text, out int _), Is.False);
        }

        [Test]
        public void TryParse_UndefinedEnumNumber_ReturnsFalse()
        {
            Assert.That(SettingValueText.TryParse("99", out FullScreenMode _), Is.False);
        }

        [Test]
        public void CanConvert_UnsupportedType_ReturnsFalse()
        {
            Assert.That(SettingValueText.CanConvert(typeof(Vector3)), Is.False);
        }
    }
}
