using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Inventory;
using UnityEngine;

namespace FakeMG.Inventory.Hud
{
    public interface IInventoryHudAnimationStep
    {
        UniTask PlayAsync(
            InventoryChange change,
            ItemCounterView counter,
            Transform rewardStartTransform,
            CancellationToken cancellationToken);
    }
}
