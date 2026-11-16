using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FakeMG.Tutorial
{
    public sealed class TimedTutorialStep : TutorialStepBase
    {
        [SerializeField, Min(0f)] private float _durationSeconds = 3f;

        protected override async UniTask WaitForCompletionAsync(TutorialContext context, CancellationToken cancellationToken)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_durationSeconds), cancellationToken: cancellationToken);
        }
    }
}
