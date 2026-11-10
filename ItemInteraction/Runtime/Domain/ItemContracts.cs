using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FakeMG.ItemInteraction
{
    public interface IInteractionItem
    {
        string DisplayName { get; }
    }

    public interface IUsableItem : IInteractionItem, IItemAnimationProfileProvider
    {
        AssetReferenceGameObject HeldPrefabReference { get; }
        Vector3 RightHandLocalPositionMeters { get; }
        Vector3 RightHandLocalRotationDegrees { get; }
        IReadOnlyList<ItemActionSO> Actions { get; }
        ItemActionSO PrimaryActionSO { get; }
    }

    public interface IConsumableItem : IUsableItem
    {
    }
}
