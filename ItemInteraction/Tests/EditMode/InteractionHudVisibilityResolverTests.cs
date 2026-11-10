using NUnit.Framework;
using UnityEngine;

namespace FakeMG.ItemInteraction.Tests
{
    public sealed class InteractionHudVisibilityResolverTests
    {
        #region Public Methods

        [Test]
        public void TabletSimulatorWithUnrecognizedMobilePlatform_ShowsUseButton()
        {
            bool isVisible = InteractionHudVisibilityResolver.IsMobileUseVisible(true, false, false, DeviceType.Handheld);

            Assert.That(isVisible, Is.True);
        }

        [TestCase(DeviceType.Handheld)]
        [TestCase(DeviceType.Unknown)]
        public void MobilePlatform_ShowsUseButton(DeviceType deviceType)
        {
            bool isVisible = InteractionHudVisibilityResolver.IsMobileUseVisible(true, false, true, deviceType);

            Assert.That(isVisible, Is.True);
        }

        [TestCase(DeviceType.Desktop)]
        [TestCase(DeviceType.Console)]
        [TestCase(DeviceType.Unknown)]
        public void NonMobileDevice_HidesUseButton(DeviceType deviceType)
        {
            bool isVisible = InteractionHudVisibilityResolver.IsMobileUseVisible(true, false, false, deviceType);

            Assert.That(isVisible, Is.False);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void NoUsableItemSelected_HidesUseButton(bool isMobilePlatform)
        {
            bool isVisible = InteractionHudVisibilityResolver.IsMobileUseVisible(false, false, isMobilePlatform, DeviceType.Handheld);

            Assert.That(isVisible, Is.False);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void InWater_HidesUseButton(bool isMobilePlatform)
        {
            bool isVisible = InteractionHudVisibilityResolver.IsMobileUseVisible(true, true, isMobilePlatform, DeviceType.Handheld);

            Assert.That(isVisible, Is.False);
        }

        #endregion
    }
}
