using FakeMG.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Base for code-defined tutorial steps. Handles the reusable lifecycle: validate
    /// required targets (resolved by key so runtime UI works), skip if already satisfied,
    /// activate modules (waiting for required and completion-blocking intros), wait for
    /// completion, then always animate modules out. Subclasses declare how the step is
    /// completed and whether it branches. Modules and required target keys are authored on
    /// the inspector.
    /// </summary>
    public abstract class TutorialStepBase : MonoBehaviour, ITutorialStep
    {
        [SerializeField] private string _stepId;
        [SerializeField] private List<TutorialTargetKeySO> _requiredTargetKeys = new();
        [SerializeReference] private List<ITutorialModule> _modules = new();

        private readonly List<ITutorialModule> _activatedModules = new();
        private readonly List<UniTask> _backgroundActivations = new();
        private SkipReason? _requiredModuleFailure;

        public StepId Id => new(_stepId);

        #region Public Methods

        [Inject]
        public void InjectModuleDependencies(IObjectResolver objectResolver)
        {
            for (int moduleIndex = 0; moduleIndex < _modules.Count; moduleIndex++)
            {
                objectResolver.Inject(_modules[moduleIndex]);
            }
        }

        public async UniTask<TutorialStepResult> ExecuteAsync(TutorialContext context, CancellationToken cancellationToken)
        {
            if (!AreRequiredTargetsAvailable(context))
            {
                Echo.Warning($"Tutorial step '{_stepId}' skipped: a required target is unavailable.");
                return TutorialStepResult.Skipped(SkipReason.MissingTarget);
            }

            if (IsAlreadySatisfied(context))
            {
                Echo.Log($"Tutorial step '{_stepId}' skipped: its completion condition is already satisfied.");
                return TutorialStepResult.Skipped(SkipReason.AlreadySatisfied);
            }

            using var stepCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            try
            {
                SkipReason? activationSkip = await ActivateModulesAsync(context, stepCts.Token);
                if (activationSkip.HasValue)
                {
                    return TutorialStepResult.Skipped(activationSkip.Value);
                }

                // Complete when the step's own conditions are met or a debug tool forces it.
                await UniTask.WhenAny(
                    WaitForCompletionAsync(context, stepCts.Token),
                    context.ForceComplete.WaitAsync(stepCts.Token));

                return BuildCompletionResult(context);
            }
            finally
            {
                stepCts.Cancel();
                // Let any non-blocking activations still in flight settle before teardown,
                // so a module that finished activating after completion is also deactivated
                // and _activatedModules is stable while DeactivateModulesAsync iterates it.
                await AwaitBackgroundActivationsAsync();
                await DeactivateModulesAsync();
            }
        }

        #endregion

        #region Subclass Hooks

        protected abstract UniTask WaitForCompletionAsync(TutorialContext context, CancellationToken cancellationToken);

        protected virtual bool IsAlreadySatisfied(TutorialContext context) => false;

        protected virtual TutorialStepResult BuildCompletionResult(TutorialContext context) => TutorialStepResult.Completed();

        #endregion

        #region Private Methods

        private bool AreRequiredTargetsAvailable(TutorialContext context)
        {
            for (int i = 0; i < _requiredTargetKeys.Count; i++)
            {
                TutorialTargetKeySO key = _requiredTargetKeys[i];
                if (!context.TargetRegistry.TryGet(key, out ITutorialTarget target) || !target.IsAvailable)
                {
                    return false;
                }
            }

            return true;
        }

        private async UniTask<SkipReason?> ActivateModulesAsync(TutorialContext context, CancellationToken cancellationToken)
        {
            _activatedModules.Clear();
            _backgroundActivations.Clear();
            _requiredModuleFailure = null;

            var awaitedActivations = new List<UniTask>();
            for (int i = 0; i < _modules.Count; i++)
            {
                ITutorialModule module = _modules[i];
                UniTask activation = ActivateModuleAsync(module, context, cancellationToken);

                if (module.BlocksCompletion || module.IsRequired)
                {
                    awaitedActivations.Add(activation);
                }
                else
                {
                    _backgroundActivations.Add(activation);
                }
            }

            await UniTask.WhenAll(awaitedActivations);
            return _requiredModuleFailure;
        }

        private async UniTask AwaitBackgroundActivationsAsync()
        {
            if (_backgroundActivations.Count == 0)
            {
                return;
            }

            // ActivateModuleAsync swallows non-cancellation failures; only cancellation
            // (from the step's CTS) can surface here, and that is the expected teardown path.
            try
            {
                await UniTask.WhenAll(_backgroundActivations);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _backgroundActivations.Clear();
            }
        }

        private async UniTask ActivateModuleAsync(ITutorialModule module, TutorialContext context, CancellationToken cancellationToken)
        {
            try
            {
                await module.ActivateAsync(context, cancellationToken);
                _activatedModules.Add(module);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                if (module.IsRequired)
                {
                    Echo.Error($"Required tutorial module failed to activate on step '{_stepId}'. Skipping step. {exception}");
                    _requiredModuleFailure = SkipReason.RequiredDependencyFailed;
                }
                else
                {
                    Echo.Warning($"Optional tutorial module failed to activate on step '{_stepId}'. Continuing without it. {exception}");
                }
            }
        }

        private async UniTask DeactivateModulesAsync()
        {
            for (int i = _activatedModules.Count - 1; i >= 0; i--)
            {
                try
                {
                    await _activatedModules[i].DeactivateAsync(CancellationToken.None);
                }
                catch (Exception exception)
                {
                    Echo.Error($"Tutorial module failed to deactivate on step '{_stepId}'. {exception}");
                }
            }

            _activatedModules.Clear();
        }

        #endregion
    }
}
