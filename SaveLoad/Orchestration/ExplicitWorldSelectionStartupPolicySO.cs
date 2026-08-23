using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.SaveLoad
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.ROOT + "/SaveLoad/Startup/Explicit World Selection")]
    public sealed class ExplicitWorldSelectionStartupPolicySO : WorldStartupPolicySO
    {
        #region Public Methods

        public override UniTask InitializeAsync(
            IWorldStartupContext worldStartupContext,
            string defaultWorldDisplayName,
            CancellationToken cancellationToken)
        {
            Echo.Log("World persistence is waiting for explicit world selection.");
            return UniTask.CompletedTask;
        }

        #endregion
    }
}
