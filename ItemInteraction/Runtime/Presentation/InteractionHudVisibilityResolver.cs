using UnityEngine;

namespace FakeMG.ItemInteraction
{
    public static class InteractionHudVisibilityResolver
    {
        #region Public Methods

        public static bool IsMobileUseVisible(bool isVisibilityRequested, bool isInWater, bool isMobilePlatform, DeviceType deviceType)
        {
            // Some Device Simulator tablet profiles report a handheld device but an unrecognized mobile OS.
            bool isMobileDevice = isMobilePlatform || deviceType == DeviceType.Handheld;
            return isVisibilityRequested && !isInWater && isMobileDevice;
        }

        #endregion
    }
}
