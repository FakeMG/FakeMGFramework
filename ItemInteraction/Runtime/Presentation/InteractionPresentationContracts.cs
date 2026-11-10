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

    public interface IWorldInteractionPointerInputSource : IInteractionPointerInputSource { }

    public interface IUsableItemInteractionPointerInputSource : IInteractionPointerInputSource { }

    public interface IItemSelectionPresentation
    {
        void ShowSelection(IInteractionItem item);
        void HideSelection();
    }

    public interface IHeldItemSelectionObserver
    {
        void HandleHeldItemShown(IInteractionItem item, GameObject heldItemObject);
        void HandleHeldItemHidden(GameObject heldItemObject);
    }

    public interface IItemUsePresentation
    {
        void BeginUse(IInteractionItem item, ItemActionSO actionSO);
        void EndUse(IInteractionItem item, ItemActionSO actionSO);
    }

    public interface ISelectedItemUseAnimation
    {
        bool TryPlaySelectedItemUseAnimation();
    }

    public interface ICharacterWaterStateListener
    {
        void SetWaterState(bool isInWater);
    }

    public interface IItemCharacterAnimation
    {
        void SetLocomotionProfile(ItemAnimationProfileSO animationProfileSO);
        void Play(ItemAnimationPose animationPose, float fadeDurationSeconds, bool shouldRestartAnimation = false);
        void Stop(float fadeDurationSeconds);
    }

    public interface ICharacterAimFacing
    {
        void SetCameraFacingOverride(bool isEnabled);
    }

    public interface IAimFieldOfView
    {
        void BeginAim(ItemCameraProfileSO cameraProfileSO);
        void EndAim();
    }
}
