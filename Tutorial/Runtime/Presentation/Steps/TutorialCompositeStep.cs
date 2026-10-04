using FakeMG.Framework;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// General-purpose step that completes when all of its inspector-authored
    /// completion conditions are satisfied. Programmers compose multiple checks
    /// without writing a bespoke step class.
    /// </summary>
    public class TutorialCompositeStep : TutorialStepBase
    {
        [SerializeReference] private List<ITutorialCompletionCondition> _completionConditions = new();

        #region Public Methods

        [Inject]
        public void InjectCompletionConditionDependencies(IObjectResolver objectResolver)
        {
            for (int conditionIndex = 0; conditionIndex < _completionConditions.Count; conditionIndex++)
            {
                objectResolver.Inject(_completionConditions[conditionIndex]);
            }
        }

        #endregion

        protected override bool IsAlreadySatisfied(TutorialContext context)
        {
            if (_completionConditions.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < _completionConditions.Count; i++)
            {
                if (!_completionConditions[i].IsAlreadySatisfied(context))
                {
                    return false;
                }
            }

            return true;
        }

        protected override async UniTask WaitForCompletionAsync(TutorialContext context, CancellationToken cancellationToken)
        {
            if (_completionConditions.Count == 0)
            {
                Echo.Warning($"Tutorial step '{Id}' has no completion conditions; completing immediately.");
                return;
            }

            var waits = new List<UniTask>(_completionConditions.Count);
            for (int i = 0; i < _completionConditions.Count; i++)
            {
                ITutorialCompletionCondition condition = _completionConditions[i];
                if (!condition.IsAlreadySatisfied(context))
                {
                    waits.Add(condition.WaitUntilSatisfiedAsync(context, cancellationToken));
                }
            }

            await UniTask.WhenAll(waits);
        }
    }
}
