using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using FakeMG.Framework.EventBus;
using VContainer;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Drives a single tutorial sequence from its first valid step to a valid end
    /// state: executes each step, records the outcome into the session's progress,
    /// and resolves the next step. Always clears the gate and releases loaded visuals
    /// when the sequence ends. Whether progress is committed is decided by the progress
    /// the service supplies (saved vs throwaway replay copy).
    /// </summary>
    public sealed class TutorialRunner
    {
        private readonly TutorialTargetRegistry _targets;
        private readonly TutorialInteractionGate _gate;
        private readonly TutorialAddressableLoader _loader;
        private readonly TutorialVisualRoot _visualRoot;
        private readonly TutorialHiddenUIRoot _hiddenUIRoot;
        private readonly TutorialForceCompleteSignal _forceCompleteSignal;
        private readonly TutorialSelectableVisibilityController _hudVisibilityController;
        private readonly IObjectResolver _objectResolver;

        public TutorialRunner(TutorialTargetRegistry targets, TutorialInteractionGate gate,
            TutorialAddressableLoader loader, TutorialVisualRoot visualRoot,
            TutorialHiddenUIRoot hiddenUIRoot, TutorialForceCompleteSignal forceCompleteSignal,
            TutorialSelectableVisibilityController hudVisibilityController, IObjectResolver objectResolver)
        {
            _targets = targets;
            _gate = gate;
            _loader = loader;
            _visualRoot = visualRoot;
            _hiddenUIRoot = hiddenUIRoot;
            _forceCompleteSignal = forceCompleteSignal;
            _hudVisibilityController = hudVisibilityController;
            _objectResolver = objectResolver;
        }

        #region Public Methods

        public async UniTask RunAsync(ITutorialSequence sequence, TutorialProgress progress,
            bool isForcedReplay, CancellationToken cancellationToken)
        {
            TutorialId tutorialId = sequence.Id;
            EventBus<TutorialStartedEvent>.Raise(new TutorialStartedEvent
            {
                TutorialId = tutorialId,
                IsForcedReplay = isForcedReplay
            });

            Echo.Log($"Tutorial '{tutorialId}' started (forced replay: {isForcedReplay}).");

            ITutorialStep step = sequence.GetFirstStep(progress);
            StepId lastStepId = default;

            // Every dependency the context carries is constant for the run; only the
            // force-complete signal is re-armed per step, in place.
            var context = new TutorialContext(tutorialId, progress, _targets, _gate, _loader,
                _visualRoot.Root, _hiddenUIRoot.Root, _forceCompleteSignal);

            try
            {
                while (step != null)
                {
                    _objectResolver.Inject(step);
                    lastStepId = step.Id;

                    EventBus<TutorialStepStartedEvent>.Raise(new TutorialStepStartedEvent
                    {
                        TutorialId = tutorialId,
                        StepId = step.Id
                    });
                    Echo.Log($"Tutorial '{tutorialId}' step '{step.Id}' started.");

                    _forceCompleteSignal.ArmForStep();
                    TutorialStepResult result = await step.ExecuteAsync(context, cancellationToken);

                    RecordResult(tutorialId, step.Id, result, progress);
                    step = sequence.GetNextStep(lastStepId, result, progress);
                }

                bool reachedValidEndState = sequence.IsValidEndState(lastStepId, progress);
                if (reachedValidEndState)
                {
                    progress.MarkTutorialCompleted(tutorialId);
                }

                EventBus<TutorialEndedEvent>.Raise(new TutorialEndedEvent
                {
                    TutorialId = tutorialId,
                    ReachedValidEndState = reachedValidEndState,
                    IsForcedReplay = isForcedReplay
                });
                Echo.Log($"Tutorial '{tutorialId}' ended (reached valid end state: {reachedValidEndState}).");
            }
            finally
            {
                _gate.ClearRestrictions();
                await _hudVisibilityController.RestoreAllAsync(CancellationToken.None);
                _loader.ReleaseAll();
            }
        }

        #endregion

        #region Private Methods

        private static void RecordResult(TutorialId tutorialId, StepId stepId, TutorialStepResult result,
            TutorialProgress progress)
        {
            if (result.Outcome == TutorialStepOutcome.Completed)
            {
                progress.MarkStepCompleted(tutorialId, stepId);
                EventBus<TutorialStepCompletedEvent>.Raise(new TutorialStepCompletedEvent
                {
                    TutorialId = tutorialId,
                    StepId = stepId
                });
                Echo.Log($"Tutorial '{tutorialId}' step '{stepId}' completed.");

                if (result.HasBranch)
                {
                    progress.RecordBranchChoice(tutorialId, stepId, result.ChosenBranch);
                    EventBus<TutorialBranchChosenEvent>.Raise(new TutorialBranchChosenEvent
                    {
                        TutorialId = tutorialId,
                        FromStepId = stepId,
                        BranchStepId = result.ChosenBranch
                    });
                    Echo.Log($"Tutorial '{tutorialId}' step '{stepId}' chose branch '{result.ChosenBranch}'.");
                }

                return;
            }

            progress.MarkStepSkipped(tutorialId, stepId, result.SkipReason);
            EventBus<TutorialStepSkippedEvent>.Raise(new TutorialStepSkippedEvent
            {
                TutorialId = tutorialId,
                StepId = stepId,
                Reason = result.SkipReason
            });
            Echo.Log($"Tutorial '{tutorialId}' step '{stepId}' skipped ({result.SkipReason}).");
        }

        #endregion
    }
}
