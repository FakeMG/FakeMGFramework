using DG.Tweening;
using UnityEngine;

namespace FakeMG.ItemInteraction
{
    public sealed class InteractionHudView : MonoBehaviour, ICharacterWaterStateListener
    {
        private const float FADE_DURATION_SECONDS = 0.15f;

        [SerializeField] private CanvasGroup _crosshairCanvasGroup;
        [SerializeField] private CanvasGroup _mobileUseCanvasGroup;

        private bool _isCrosshairVisibilityRequested;
        private bool _isMobileUseVisibilityRequested;
        private bool _isGameplayInputAvailable = true;
        private bool _isInWater;

        #region Unity Lifecycle

        private void Awake()
        {
            SetCanvasGroupState(_crosshairCanvasGroup, false, true);
            SetCanvasGroupState(_mobileUseCanvasGroup, false, true);
        }

        private void OnDisable()
        {
            _crosshairCanvasGroup.DOKill();
            _mobileUseCanvasGroup.DOKill();
        }

        #endregion

        #region Public Methods

        public void SetCrosshairVisible(bool isVisible)
        {
            if (_isCrosshairVisibilityRequested == isVisible) return;
            _isCrosshairVisibilityRequested = isVisible;
            ApplyCrosshairVisibility(false);
        }

        public void SetMobileUseVisible(bool isVisible)
        {
            if (_isMobileUseVisibilityRequested == isVisible) return;
            _isMobileUseVisibilityRequested = isVisible;
            ApplyMobileUseVisibility(false);
        }

        public void SetGameplayInputAvailable(bool isAvailable)
        {
            _isGameplayInputAvailable = isAvailable;
            ApplyMobileInputAvailability();
        }

        public void SetWaterState(bool isInWater)
        {
            _isInWater = isInWater;
            ApplyCrosshairVisibility(false);
            ApplyMobileUseVisibility(false);
        }

        #endregion

        #region Private Methods

        private void ApplyCrosshairVisibility(bool isImmediate)
        {
            SetCanvasGroupState(_crosshairCanvasGroup, _isCrosshairVisibilityRequested && !_isInWater, isImmediate);
        }

        private void ApplyMobileUseVisibility(bool isImmediate)
        {
            SetCanvasGroupState(_mobileUseCanvasGroup, IsMobileUseVisible(), isImmediate);
            ApplyMobileInputAvailability();
        }

        private void ApplyMobileInputAvailability()
        {
            bool canReceiveInput = _isGameplayInputAvailable && IsMobileUseVisible();
            _mobileUseCanvasGroup.interactable = canReceiveInput;
            _mobileUseCanvasGroup.blocksRaycasts = canReceiveInput;
        }

        private bool IsMobileUseVisible()
        {
            return InteractionHudVisibilityResolver.IsMobileUseVisible(_isMobileUseVisibilityRequested, _isInWater,
                UnityEngine.Device.Application.isMobilePlatform, UnityEngine.Device.SystemInfo.deviceType);
        }

        private static void SetCanvasGroupState(CanvasGroup canvasGroup, bool isVisible, bool isImmediate)
        {
            canvasGroup.DOKill();
            canvasGroup.interactable = isVisible;
            canvasGroup.blocksRaycasts = isVisible;
            if (isImmediate)
            {
                canvasGroup.alpha = isVisible ? 1f : 0f;
                return;
            }

            canvasGroup.DOFade(isVisible ? 1f : 0f, FADE_DURATION_SECONDS)
                .SetUpdate(true)
                .SetLink(canvasGroup.gameObject);
        }

        #endregion
    }
}
