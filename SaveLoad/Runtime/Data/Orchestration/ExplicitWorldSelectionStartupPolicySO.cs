using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.SaveLoad
{
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
