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
        [Tooltip("Optional clips played in order after Use Item, such as a bow's release after its draw.")]
        [AssetsOnly]
        [SerializeField] private AnimationClip[] _followUpUseAnimationClips = Array.Empty<AnimationClip>();
        [Tooltip("Spine rotation correction in degrees when Use Item is layered over locomotion.")]
        [SerializeField] private Vector3 _useUpperBodyRotationOffsetDegrees;

        [Header("Playback")]
        [Tooltip("Blend duration between item poses, in seconds.")]
        [MinValue(0f)]
        [SerializeField] private float _fadeDurationSeconds = 0.2f;
        [Tooltip("Normalized time (0-1) across the whole use sequence at which the item's action takes effect.")]
        [Range(0f, 1f)]
        [SerializeField] private float _actionMomentNormalized = 0.65f;

        public AnimationClip HoldingIdleAnimationClip => _holdingIdleAnimationClip;
        public AnimationClip UseAnimationClip => _useAnimationClip;
        public float FadeDurationSeconds => _fadeDurationSeconds;
        public float ActionMomentNormalized => _actionMomentNormalized;
        public float ActionMomentSeconds => UseDurationSeconds * _actionMomentNormalized;

        public float UseDurationSeconds
        {
            get
            {
                float durationSeconds = _useAnimationClip.length;
                foreach (AnimationClip followUpClip in _followUpUseAnimationClips)
                {
                    durationSeconds += followUpClip.length;
                }

                return durationSeconds;
            }
        }

        #region Public Methods

        public ItemAnimationPose GetHoldingPose()
        {
            return new ItemAnimationPose(_holdingIdleAnimationClip, _holdingIdleUpperBodyRotationOffsetDegrees, playbackSpeedMultiplier: 1f);
        }

        public ItemAnimationPose GetUsePose(int useClipIndex, float playbackSpeedMultiplier)
        {
            AnimationClip useClip = useClipIndex <= 0 ? _useAnimationClip : _followUpUseAnimationClips[useClipIndex - 1];
            return new ItemAnimationPose(useClip, _useUpperBodyRotationOffsetDegrees, playbackSpeedMultiplier);
        }

        /// <summary>
        /// Resolves which clip of the use sequence is playing after the given unscaled clip time.
        /// Time past the end of the sequence stays on its last clip.
        /// </summary>
        public int GetUseClipIndex(float useClipTimeSeconds)
        {
            float clipEndSeconds = _useAnimationClip.length;
            for (int followUpIndex = 0; followUpIndex < _followUpUseAnimationClips.Length; followUpIndex++)
            {
                if (useClipTimeSeconds < clipEndSeconds)
                {
                    return followUpIndex;
                }

                clipEndSeconds += _followUpUseAnimationClips[followUpIndex].length;
            }

            return _followUpUseAnimationClips.Length;
        }

        public void ValidateOrThrow()
        {
            if (_holdingIdleAnimationClip == null || _useAnimationClip == null)
            {
                throw new InvalidOperationException($"{name} requires Holding Idle and Use Item animation clips.");
            }

            if (Array.IndexOf(_followUpUseAnimationClips, null) >= 0)
            {
                throw new InvalidOperationException($"{name} contains a missing follow-up Use Item animation clip.");
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

        public void ConfigureFollowUpUseAnimationsForEditor(params AnimationClip[] followUpUseAnimationClips)
        {
            _followUpUseAnimationClips = followUpUseAnimationClips;
        }

        public void ConfigureRotationOffsetsForEditor(Vector3 holdingIdleOffsetDegrees, Vector3 useOffsetDegrees)
        {
            _holdingIdleUpperBodyRotationOffsetDegrees = holdingIdleOffsetDegrees;
            _useUpperBodyRotationOffsetDegrees = useOffsetDegrees;
        }

        public void ConfigurePlaybackForEditor(float fadeDurationSeconds, float actionMomentNormalized)
        {
            _fadeDurationSeconds = fadeDurationSeconds;
            _actionMomentNormalized = actionMomentNormalized;
        }
#endif

        #endregion
    }
}
