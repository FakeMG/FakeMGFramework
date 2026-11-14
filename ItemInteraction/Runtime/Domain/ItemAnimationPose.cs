using UnityEngine;

namespace FakeMG.ItemInteraction
{
    public readonly struct ItemAnimationPose
    {
        public AnimationClip AnimationClip { get; }
        public Vector3 UpperBodyRotationOffsetDegrees { get; }
        public float PlaybackSpeedMultiplier { get; }

        public ItemAnimationPose(AnimationClip animationClip, Vector3 upperBodyRotationOffsetDegrees, float playbackSpeedMultiplier)
        {
            AnimationClip = animationClip;
            UpperBodyRotationOffsetDegrees = upperBodyRotationOffsetDegrees;
            PlaybackSpeedMultiplier = playbackSpeedMultiplier;
        }
    }
}
