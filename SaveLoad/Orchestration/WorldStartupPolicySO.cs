using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.SaveLoad
{
    public abstract class WorldStartupPolicySO : ScriptableObject
    {
        #region Public Methods

        public abstract UniTask<StartupReadinessResult> InitializeAsync(
            IWorldStartupContext worldStartupContext,
            string defaultWorldDisplayName,
            CancellationToken cancellationToken);

        #endregion
    }
}
