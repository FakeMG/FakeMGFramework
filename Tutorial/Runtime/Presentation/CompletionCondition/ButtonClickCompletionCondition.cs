using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace FakeMG.Tutorial
{
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
