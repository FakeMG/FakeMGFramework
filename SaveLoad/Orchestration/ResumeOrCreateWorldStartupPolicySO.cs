using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.SaveLoad
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.ROOT + "/SaveLoad/Startup/Resume Or Create World")]
    public sealed class ResumeOrCreateWorldStartupPolicySO : WorldStartupPolicySO
    {
        #region Public Methods

        public override async UniTask InitializeAsync(
            IWorldStartupContext worldStartupContext,
            string defaultWorldDisplayName,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<WorldSummary> worlds = worldStartupContext.GetWorlds();
            foreach (WorldSummary world in worlds)
            {
                WorldOperationResult openResult = await worldStartupContext.OpenWorldAsync(
                    world.WorldId,
                    cancellationToken);
                if (openResult.Succeeded)
                {
                    return;
                }

                Echo.Warning($"Could not resume world '{world.WorldId}'. Trying the next world.");
            }

            WorldCreationResult creationResult = await worldStartupContext.CreateWorldAsync(
                defaultWorldDisplayName,
                cancellationToken);
            if (!creationResult.Succeeded)
            {
                Echo.Error($"Could not create the default world: {creationResult.FailureReason}");
            }
        }

        #endregion
    }
}
