using FakeMG.Framework;
using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FakeMG.ItemInteraction
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.ITEM_INTERACTION + "/Animation Profile")]
    public class ItemAnimationProfileSO : ScriptableObject
    {
        [Header("Holding Idle - Required")]
        [Tooltip("Upper-body holding animation, blended over the current base locomotion in every state.")]
        [AssetsOnly, Required]
        [SerializeField] private AnimationClip _holdingIdleAnimationClip;
        [Tooltip("Spine rotation correction in degrees for Holding Idle in every locomotion state.")]
        [SerializeField] private Vector3 _holdingIdleUpperBodyRotationOffsetDegrees;

        [Header("Use Item - Required")]
        [Tooltip("Upper-body use animation, blended over the current base locomotion in every state.")]
        [AssetsOnly, Required]
        [SerializeField] private AnimationClip _useAnimationClip;
        [Tooltip("Spine rotation correction in degrees when Use Item is layered over locomotion.")]
        [SerializeField] private Vector3 _useUpperBodyRotationOffsetDegrees;

        [Header("Playback")]
        [Tooltip("Blend duration between item poses, in seconds.")]
        [MinValue(0f)]
        [SerializeField] private float _fadeDurationSeconds = 0.2f;
        [Tooltip("Normalized time (0-1) at which the item's action takes effect.")]
        [Range(0f, 1f)]
        [SerializeField] private float _actionMomentNormalized = 0.65f;

        public AnimationClip HoldingIdleAnimationClip => _holdingIdleAnimationClip;
        public AnimationClip UseAnimationClip => _useAnimationClip;
        public float FadeDurationSeconds => _fadeDurationSeconds;
        public float ActionMomentNormalized => _actionMomentNormalized;

        #region Public Methods

        public ItemAnimationPose GetHoldingPose()
        {
            return new ItemAnimationPose(_holdingIdleAnimationClip, _holdingIdleUpperBodyRotationOffsetDegrees);
        }

        public ItemAnimationPose GetUsePose()
        {
            return new ItemAnimationPose(_useAnimationClip, _useUpperBodyRotationOffsetDegrees);
        }

        public void ValidateOrThrow()
        {
            if (_holdingIdleAnimationClip == null || _useAnimationClip == null)
            {
                throw new InvalidOperationException($"{name} requires Holding Idle and Use Item animation clips.");
            }

            if (_fadeDurationSeconds < 0f || _actionMomentNormalized < 0f || _actionMomentNormalized > 1f)
            {
                throw new InvalidOperationException($"{name} requires a non-negative fade duration and an action moment between 0 and 1.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureAnimationsForEditor(AnimationClip holdingIdleAnimationClip, AnimationClip useAnimationClip)
        {
            _holdingIdleAnimationClip = holdingIdleAnimationClip;
            _useAnimationClip = useAnimationClip;
        }

        public void ConfigureRotationOffsetsForEditor(Vector3 holdingIdleOffsetDegrees, Vector3 useOffsetDegrees)
        {
            _holdingIdleUpperBodyRotationOffsetDegrees = holdingIdleOffsetDegrees;
            _useUpperBodyRotationOffsetDegrees = useOffsetDegrees;
        }

#endif

        #endregion
    }
}
