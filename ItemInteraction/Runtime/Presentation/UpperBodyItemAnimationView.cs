using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.ItemInteraction
{
    public sealed class UpperBodyItemAnimationView :
        MonoBehaviour,
        IItemSelectionPresentation,
        IItemUsePresentation,
        ISelectedItemUseAnimation,
        ICharacterWaterStateListener
    {
        private const float STOP_FADE_DURATION_SECONDS = 0.2f;

        private readonly ItemUsePlayhead _usePlayhead = new();
        private IItemCharacterAnimation _itemCharacterAnimation;
        private IInteractionItem _selectedItem;
        // Null when the selected item has no holding pose, such as the empty hand, which keeps the base locomotion.
        private ItemAnimationProfileSO _holdingAnimationProfileSO;
        private ItemActionSO _activeActionSO;
        private bool _isInWater;

        #region Unity Lifecycle

        private void Update()
        {
            if (_isInWater)
            {
                return;
            }

            if (_usePlayhead.IsPlaying)
            {
                AdvanceUseAnimation();
                return;
            }

            // Replayed every frame because whether the character layers its holding pose can change with its locomotion state.
            if (_holdingAnimationProfileSO != null)
            {
                PlayHoldingPose();
            }
        }

        #endregion

        #region Public Methods

        public void Bind(IItemCharacterAnimation itemCharacterAnimation)
        {
            _itemCharacterAnimation = itemCharacterAnimation;
        }

        public void SetWaterState(bool isInWater)
        {
            _isInWater = isInWater;
            if (!isInWater)
            {
                ShowCurrentAnimation(STOP_FADE_DURATION_SECONDS);
                return;
            }

            // An interaction's use resumes after the water; a one-off use animation does not.
            if (_activeActionSO == null)
            {
                _usePlayhead.Stop();
            }

            _itemCharacterAnimation.Stop(STOP_FADE_DURATION_SECONDS);
        }

        public void ShowSelection(IInteractionItem item)
        {
            _selectedItem = item;
            _holdingAnimationProfileSO = (item as IItemAnimationProfileProvider)?.AnimationProfileSO;
            _activeActionSO = null;
            _usePlayhead.Stop();
            ShowCurrentAnimation(STOP_FADE_DURATION_SECONDS);
        }

        public void HideSelection()
        {
            _selectedItem = null;
            _holdingAnimationProfileSO = null;
            _activeActionSO = null;
            _usePlayhead.Stop();
            _itemCharacterAnimation.Stop(STOP_FADE_DURATION_SECONDS);
        }

        public void BeginUse(IInteractionItem item, ItemActionSO actionSO, float playbackSpeedMultiplier)
        {
            if (_selectedItem != item)
            {
                Echo.Warning($"Ignoring use presentation for {item.DisplayName} because it is not selected.");
                return;
            }

            _activeActionSO = actionSO;
            _usePlayhead.Start(actionSO.AnimationProfileSO, playbackSpeedMultiplier, stopsAtSequenceEnd: false);
            ShowUseAnimationFromStart();
        }

        public void EndUse(IInteractionItem item, ItemActionSO actionSO)
        {
            if (_selectedItem != item || _activeActionSO != actionSO)
            {
                Echo.Warning($"Ignoring stale end-use presentation for {item.DisplayName}.");
                return;
            }

            _activeActionSO = null;
            _usePlayhead.Stop();
            ShowCurrentAnimation(actionSO.AnimationProfileSO.FadeDurationSeconds);
        }

        public bool TryPlaySelectedItemUseAnimation()
        {
            if (_isInWater || _holdingAnimationProfileSO == null || _activeActionSO != null)
            {
                return false;
            }

            _usePlayhead.Start(_holdingAnimationProfileSO, playbackSpeedMultiplier: 1f, stopsAtSequenceEnd: true);
            ShowUseAnimationFromStart();
            return true;
        }

        #endregion

        #region Private Methods

        private void ShowCurrentAnimation(float stopFadeDurationSeconds)
        {
            if (_isInWater)
            {
                return;
            }

            _itemCharacterAnimation.SetLocomotionProfile(_holdingAnimationProfileSO);
            if (_usePlayhead.IsPlaying)
            {
                PlayUsePose(shouldRestartAnimation: false);
            }
            else if (_holdingAnimationProfileSO != null)
            {
                PlayHoldingPose();
            }
            else
            {
                _itemCharacterAnimation.Stop(stopFadeDurationSeconds);
            }
        }

        private void ShowUseAnimationFromStart()
        {
            if (_isInWater)
            {
                return;
            }

            _itemCharacterAnimation.SetLocomotionProfile(_holdingAnimationProfileSO);
            PlayUsePose(shouldRestartAnimation: true);
        }

        private void AdvanceUseAnimation()
        {
            bool hasReachedNextClip = _usePlayhead.Advance(Time.deltaTime);
            if (_usePlayhead.HasFinished)
            {
                float fadeDurationSeconds = _usePlayhead.AnimationProfileSO.FadeDurationSeconds;
                _usePlayhead.Stop();
                ShowCurrentAnimation(fadeDurationSeconds);
                return;
            }

            if (hasReachedNextClip)
            {
                PlayUsePose(shouldRestartAnimation: true);
            }
        }

        private void PlayUsePose(bool shouldRestartAnimation)
        {
            _itemCharacterAnimation.PlayUsePose(
                _usePlayhead.CurrentPose,
                _usePlayhead.AnimationProfileSO.FadeDurationSeconds,
                shouldRestartAnimation);
        }

        private void PlayHoldingPose()
        {
            _itemCharacterAnimation.PlayHoldingPose(
                _holdingAnimationProfileSO.GetHoldingPose(),
                _holdingAnimationProfileSO.FadeDurationSeconds);
        }

        #endregion
    }
}
