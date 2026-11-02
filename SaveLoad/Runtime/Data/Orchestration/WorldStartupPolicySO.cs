using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.SaveLoad
{
    public abstract class WorldStartupPolicySO : ScriptableObject
    {
        /// <summary>
        /// When true, startup only reports ready after this policy has made a world active.
        /// Policies that defer world selection to the player (e.g. a main menu) return false.
        /// </summary>
        public virtual bool OpensWorldOnStartup => true;

        #region Public Methods

        public abstract UniTask<StartupReadinessResult> InitializeAsync(
            IWorldStartupContext worldStartupContext,
            string defaultWorldDisplayName,
            CancellationToken cancellationToken);

        #endregion
    }
}
