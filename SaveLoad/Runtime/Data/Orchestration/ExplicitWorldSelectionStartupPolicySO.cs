using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.SaveLoad
{
    /// <summary>
    /// Startup becomes ready once global saves are initialized, with no active world.
    /// The game is expected to create or open a world explicitly before loading gameplay.
    /// </summary>
    [CreateAssetMenu(menuName = FakeMGEditorMenus.ROOT + "/SaveLoad/Startup/Explicit World Selection")]
    public sealed class ExplicitWorldSelectionStartupPolicySO : WorldStartupPolicySO
    {
        public override bool OpensWorldOnStartup => false;

        #region Public Methods

        public override UniTask<StartupReadinessResult> InitializeAsync(
            IWorldStartupContext worldStartupContext,
            string defaultWorldDisplayName,
            CancellationToken cancellationToken)
        {
            Echo.Log("World persistence is ready and waiting for explicit world selection.");
            return UniTask.FromResult(StartupReadinessResult.Ready());
        }

        #endregion
    }
}
