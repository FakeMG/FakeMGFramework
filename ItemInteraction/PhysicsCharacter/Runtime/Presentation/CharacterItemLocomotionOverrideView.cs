using PhysicsCharacterController;
using UnityEngine;

namespace FakeMG.ItemInteraction
{
    public sealed class CharacterItemLocomotionOverrideView : MonoBehaviour
    {
        [SerializeField] private CharacterAnimator _characterAnimator;

        private CharacterItemAnimationProfileSO _profileSO;

        /// <summary>
        /// Whether the item's locomotion override is playing now and already holds the item,
        /// so the holding pose must be released instead of layered over it.
        /// </summary>
        public bool IsHoldingPoseReplacedByLocomotionOverride =>
            _profileSO != null && _profileSO.IsHoldingPoseReplacedByLocomotionOverride && _characterAnimator.HasLocomotionOverride;

        #region Public Methods

        public void SetLocomotionProfile(ItemAnimationProfileSO animationProfileSO)
        {
            var profileSO = animationProfileSO as CharacterItemAnimationProfileSO;
            if (_profileSO == profileSO)
            {
                return;
            }

            _profileSO = profileSO;
            _characterAnimator.SetLocomotionOverride(profileSO);
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(CharacterAnimator characterAnimator)
        {
            _characterAnimator = characterAnimator;
        }
#endif

        #endregion
    }
}
