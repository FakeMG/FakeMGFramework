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
        private IItemCharacterAnimation _itemCharacterAnimation;
        private IInteractionItem _selectedItem;
        private IItemAnimationProfileProvider _selectedAnimationProfileProvider;
        private ItemActionSO _activeActionSO;
        private float _selectedItemUseRemainingSeconds;
        private bool _isInWater;

        #region Unity Lifecycle

        private void Update()
        {
            if (_isInWater || _selectedItem == null)
            {
                return;
            }

            if (_activeActionSO != null)
            {
                PlayUseAnimation(_activeActionSO.AnimationProfileSO);
                return;
            }

            if (_selectedItemUseRemainingSeconds > 0f)
            {
                _selectedItemUseRemainingSeconds = Mathf.Max(0f, _selectedItemUseRemainingSeconds - Time.deltaTime);
                if (_selectedItemUseRemainingSeconds > 0f)
                {
                    PlayUseAnimation(_selectedAnimationProfileProvider.AnimationProfileSO);
                    return;
                }
            }

            RefreshSelectionAnimation();
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
            if (isInWater)
            {
                _selectedItemUseRemainingSeconds = 0f;
                _itemCharacterAnimation.Stop(0.2f);
                return;
            }

            ResumeAnimationAfterLeavingWater();
        }

        public void ShowSelection(IInteractionItem item)
        {
            if (item is not IItemAnimationProfileProvider animationProfileProvider)
            {
                return;
            }

            _selectedItem = item;
            _selectedAnimationProfileProvider = animationProfileProvider;
            _activeActionSO = null;
            _selectedItemUseRemainingSeconds = 0f;
            _itemCharacterAnimation.SetLocomotionProfile(animationProfileProvider.AnimationProfileSO);
            RefreshSelectionAnimation();
        }

        public void HideSelection()
        {
            _selectedItem = null;
            _selectedAnimationProfileProvider = null;
            _activeActionSO = null;
            _selectedItemUseRemainingSeconds = 0f;
            _itemCharacterAnimation.Stop(0.2f);
        }

        public void BeginUse(IInteractionItem item, ItemActionSO actionSO)
        {
            if (_selectedItem != item)
            {
                Echo.Warning($"Ignoring use presentation for {item.DisplayName} because it is not selected.");
                return;
            }

            _activeActionSO = actionSO;
            _selectedItemUseRemainingSeconds = 0f;
            if (_isInWater)
            {
                return;
            }

            PlayUseAnimation(actionSO.AnimationProfileSO);
        }

        public void EndUse(IInteractionItem item, ItemActionSO actionSO)
        {
            if (_selectedItem != item || _activeActionSO != actionSO)
            {
                Echo.Warning($"Ignoring stale end-use presentation for {item.DisplayName}.");
                return;
            }

            _activeActionSO = null;
            RefreshSelectionAnimation();
        }

        public bool TryPlaySelectedItemUseAnimation()
        {
            if (_isInWater || _selectedAnimationProfileProvider == null || _activeActionSO != null)
            {
                return false;
            }

            ItemAnimationProfileSO animationProfileSO = _selectedAnimationProfileProvider.AnimationProfileSO;
            AnimationClip useAnimationClip = animationProfileSO.UseAnimationClip;
            _selectedItemUseRemainingSeconds = useAnimationClip.length;
            PlayUseAnimation(animationProfileSO, shouldRestartAnimation: true);
            return true;
        }

        #endregion

        #region Private Methods

        private void ResumeAnimationAfterLeavingWater()
        {
            if (_activeActionSO != null)
            {
                PlayUseAnimation(_activeActionSO.AnimationProfileSO);
                return;
            }

            if (_selectedItem != null)
            {
                RefreshSelectionAnimation();
            }
        }

        private void RefreshSelectionAnimation()
        {
            ItemAnimationProfileSO animationProfileSO = _selectedAnimationProfileProvider.AnimationProfileSO;
            _itemCharacterAnimation.SetLocomotionProfile(animationProfileSO);
            ItemAnimationPose animationPose = animationProfileSO.GetHoldingPose();
            PlayItemAnimation(animationPose, animationProfileSO.FadeDurationSeconds);
        }

        private void PlayUseAnimation(ItemAnimationProfileSO animationProfileSO, bool shouldRestartAnimation = false)
        {
            _itemCharacterAnimation.SetLocomotionProfile(_selectedAnimationProfileProvider.AnimationProfileSO);
            ItemAnimationPose animationPose = animationProfileSO.GetUsePose();
            PlayItemAnimation(animationPose, animationProfileSO.FadeDurationSeconds, shouldRestartAnimation);
        }

        private void PlayItemAnimation(ItemAnimationPose animationPose, float fadeDurationSeconds, bool shouldRestartAnimation = false)
        {
            if (_isInWater)
            {
                return;
            }

            _itemCharacterAnimation.Play(animationPose, fadeDurationSeconds, shouldRestartAnimation);
        }

        #endregion
    }
}
