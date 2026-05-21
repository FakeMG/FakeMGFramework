using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FakeMG.Inventory.Hud
{
    public sealed class InventoryHudAnimationSequence
    {
        private readonly IInventoryHudAnimationStep[] _steps;

        public InventoryHudAnimationSequence(IEnumerable<IInventoryHudAnimationStep> steps)
        {
            _steps = steps.ToArray();
        }

        #region Public Methods

        public async UniTask PlayAsync(
            InventoryChange change,
            ItemCounterView counter,
            Transform rewardStartTransform,
            CancellationToken cancellationToken)
        {
            for (int stepIndex = 0; stepIndex < _steps.Length; stepIndex++)
            {
                await _steps[stepIndex].PlayAsync(change, counter, rewardStartTransform, cancellationToken);
            }
        }

        #endregion
    }
}
