using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FakeMG.Tutorial
{
    [Serializable]
    public abstract class GameplayStateSetupBehaviorModule : ITutorialModule
    {
        [SerializeField] private bool _isRequired = true;

        public bool BlocksCompletion => true;
        public bool IsRequired => _isRequired;

        /// <summary>
        /// Return false (after logging why) when the required gameplay state cannot be
        /// set up; the step then reacts per IsRequired.
        /// </summary>
        public abstract UniTask<bool> ActivateAsync(TutorialContext context, CancellationToken cancellationToken);
        public abstract UniTask DeactivateAsync(CancellationToken cancellationToken);
    }
}
