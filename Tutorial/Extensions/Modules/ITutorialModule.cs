using System.Threading;
using Cysharp.Threading.Tasks;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Reusable unit of tutorial presentation or behavior activated by a step. Modules
    /// animate or set up on activate and restore or animate out on deactivate.
    /// </summary>
    public interface ITutorialModule
    {
        /// <summary>
        /// When true, the step must wait for ActivateAsync to finish before accepting
        /// completion input.
        /// </summary>
        bool BlocksCompletion { get; }

        /// <summary>
        /// When true, a failure to activate this module skips the step. When false, the
        /// failure is logged and the step continues.
        /// </summary>
        bool IsRequired { get; }

        UniTask ActivateAsync(TutorialContext context, CancellationToken cancellationToken);
        UniTask DeactivateAsync(CancellationToken cancellationToken);
    }
}
