using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Completion condition satisfied when the player clicks a specific scene button, e.g.
    /// a persistent "continue" button. For runtime UI (popups) or non-Button targets, use
    /// <see cref="TargetClickCompletionCondition"/>.
    /// </summary>
    [Serializable]
    public sealed class ButtonClickCompletionCondition : ITutorialCompletionCondition
    {
        [SerializeField] private Button _button;

        public bool IsAlreadySatisfied(TutorialContext context) => false;

        public async UniTask WaitUntilSatisfiedAsync(TutorialContext context, CancellationToken cancellationToken)
        {
            await _button.OnClickAsync(cancellationToken);
        }
    }
}
