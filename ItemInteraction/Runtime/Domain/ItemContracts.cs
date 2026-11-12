using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FakeMG.ItemInteraction
{
    public interface IInteractionItem
    {
        string DisplayName { get; }
    }

    public enum HeldItemHand
    {
        Right,
        Left,
    }

    /// <summary>
    /// Where a held prefab sits relative to the socket of the hand holding it.
    /// </summary>
    [Serializable]
    public struct HandPose
    {
        [Tooltip("Offset from the holding hand in meters.")]
        [SerializeField] private Vector3 _localPositionMeters;

        [Tooltip("Rotation relative to the holding hand in degrees.")]
        [SerializeField] private Vector3 _localRotationDegrees;

        public Vector3 LocalPositionMeters => _localPositionMeters;
        public Vector3 LocalRotationDegrees => _localRotationDegrees;

        public HandPose(Vector3 localPositionMeters, Vector3 localRotationDegrees)
        {
            _localPositionMeters = localPositionMeters;
            _localRotationDegrees = localRotationDegrees;
        }
    }

    /// <summary>
    /// An item that can perform actions. It need not be something in the hand: the empty hand is one too.
    /// </summary>
    public interface IActionItem : IInteractionItem
    {
        IReadOnlyList<ItemActionSO> Actions { get; }
        ItemActionSO PrimaryActionSO { get; }
    }

    /// <summary>
    /// An action item held in one hand as a prefab, with its own holding pose.
    /// </summary>
    public interface IHeldItem : IActionItem, IItemAnimationProfileProvider
    {
        AssetReferenceGameObject HeldPrefabReference { get; }
        HeldItemHand HoldingHand { get; }
        HandPose HoldingHandPose { get; }
    }

    public interface IConsumableItem : IHeldItem
    {
    }
}
