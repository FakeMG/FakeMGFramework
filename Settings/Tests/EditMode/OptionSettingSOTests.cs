using FakeMG.Settings.Display;
using NUnit.Framework;
using UnityEngine;

namespace FakeMG.Settings.Tests
{
    public sealed class OptionSettingSOTests
    {
        private WindowModeOptionSettingSO _windowModeSettingSO;
        private FrameRateOptionSettingSO _frameRateSettingSO;

        [SetUp]
        public void SetUp()
        {
            _windowModeSettingSO = ScriptableObject.CreateInstance<WindowModeOptionSettingSO>();
            _frameRateSettingSO = ScriptableObject.CreateInstance<FrameRateOptionSettingSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_windowModeSettingSO);
            Object.DestroyImmediate(_frameRateSettingSO);
        }

        [Test]
        public void IndexOfOptionOrDefault_KnownId_ReturnsItsIndex()
        {
            Assert.That(_windowModeSettingSO.IndexOfOptionOrDefault(nameof(FullScreenMode.Windowed)), Is.EqualTo(2));
        }

        /// <remarks>Regression guard: saved display labels such as "Borderless" from older builds must not break loading.</remarks>
        [TestCase("Borderless")]
        [TestCase("")]
        [TestCase(null)]
        public void IndexOfOptionOrDefault_UnknownId_ReturnsFirstOption(string optionId)
        {
            Assert.That(_windowModeSettingSO.IndexOfOptionOrDefault(optionId), Is.Zero);
        }

        [Test]
        public void GetFullScreenMode_OptionId_ReturnsThatMode()
        {
            Assert.That(_windowModeSettingSO.GetFullScreenMode(nameof(FullScreenMode.FullScreenWindow)), Is.EqualTo(FullScreenMode.FullScreenWindow));
        }

        [Test]
        public void GetOptions_WindowModes_ShowLabelsAndSaveIds()
        {
            SettingOption borderlessOption = _windowModeSettingSO.GetOptions()[1];

            Assert.That(borderlessOption.Label, Is.EqualTo("Borderless"));
            Assert.That(borderlessOption.Id, Is.EqualTo(nameof(FullScreenMode.FullScreenWindow)));
        }

        [Test]
        public void GetFrameRateFps_OptionId_ReturnsThatRate()
        {
            Assert.That(_frameRateSettingSO.GetFrameRateFps("144"), Is.EqualTo(144));
        }

        [Test]
        public void GetFrameRateFps_UnknownId_ReturnsFirstOption()
        {
            Assert.That(_frameRateSettingSO.GetFrameRateFps("144 FPS"), Is.EqualTo(30));
        }
    }
}
