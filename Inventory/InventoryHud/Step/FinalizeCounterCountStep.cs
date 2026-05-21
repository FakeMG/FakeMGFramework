using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FakeMG.Inventory.Hud
{
    public sealed class FinalizeCounterCountStep : IInventoryHudAnimationStep
    {
        #region Public Methods

        public UniTask PlayAsync(
            InventoryChange change,
            ItemCounterView counter,
            Transform rewardStartTransform,
            CancellationToken cancellationToken)
        {
            counter.SetCountImmediately(change.NewCount);
            return UniTask.CompletedTask;
        }

        #endregion
    }
}
