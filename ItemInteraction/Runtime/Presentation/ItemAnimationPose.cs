using UnityEngine;

namespace FakeMG.ItemInteraction
{
    public readonly struct ItemAnimationPose
    {
        public AnimationClip AnimationClip { get; }
        public Vector3 UpperBodyRotationOffsetDegrees { get; }

        public ItemAnimationPose(AnimationClip animationClip, Vector3 upperBodyRotationOffsetDegrees)
        {
            AnimationClip = animationClip;
            UpperBodyRotationOffsetDegrees = upperBodyRotationOffsetDegrees;
        }
    }
}
