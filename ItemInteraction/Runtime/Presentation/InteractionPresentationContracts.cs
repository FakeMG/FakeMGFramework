using System;
using UnityEngine;

namespace FakeMG.ItemInteraction
{
    public interface IInteractionPointerInputSource
    {
        bool IsPressed { get; }
        Vector2 ScreenPositionPixels { get; }

        event Action<IInteractionPointerInputSource> OnPressed;
        event Action<IInteractionPointerInputSource> OnReleased;
    }

    public interface IUsableItemInteractionPointerInputSource : IInteractionPointerInputSource { }

    public interface IItemSelectionPresentation
    {
        void ShowSelection(IInteractionItem item);
        void HideSelection();
    }

    /// <summary>
    /// Grips a shown held item with the hand that is not holding it.
    /// </summary>
    public interface IOffHandGripPresentation
    {
        void BeginOffHandGrip(HeldItemHand holdingHand, GameObject heldItemInstance);
        void ReleaseOffHandGrip();
    }

    public interface ISelectedItemUseAnimation
    {
        /// <summary>
        /// Plays the selected held item's use sequence once, outside any interaction, such as a gun's recoil on each shot.
        /// </summary>
        bool TryPlaySelectedItemUseAnimation();
    }

    public interface ICharacterWaterStateListener
    {
        void SetWaterState(bool isInWater);
    }

    public interface IItemCharacterAnimation
    {
        /// <summary>
        /// The selected item's profile, for characters that change more than the item poses for it; null for none.
        /// </summary>
        void SetLocomotionProfile(ItemAnimationProfileSO animationProfileSO);

        /// <summary>
        /// Holds the item between uses. A character may layer it differently from a use, such as with the arms only.
        /// </summary>
        void PlayHoldingPose(ItemAnimationPose animationPose, float fadeDurationSeconds);

        void PlayUsePose(ItemAnimationPose animationPose, float fadeDurationSeconds, bool shouldRestartAnimation);
        void Stop(float fadeDurationSeconds);
    }
}
