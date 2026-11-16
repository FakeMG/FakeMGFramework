using FakeMG.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FakeMG.Tutorial
{
    [Serializable]
    public sealed class InputRestrictionBehaviorModule : ITutorialModule
    {
        [SerializeField] private bool _isRequired = true;
        [SerializeField] private List<TutorialTargetKeySO> _allowedTargetKeys = new();

        private TutorialInteractionGate _gate;

        public bool BlocksCompletion => false;
        public bool IsRequired => _isRequired;

        public UniTask<bool> ActivateAsync(TutorialContext context, CancellationToken cancellationToken)
        {
            _gate = context.Gate;

            var resolvedTargets = new List<ITutorialTarget>(_allowedTargetKeys.Count);
            for (int i = 0; i < _allowedTargetKeys.Count; i++)
            {
                TutorialTargetKeySO key = _allowedTargetKeys[i];
                if (context.TargetRegistry.TryGet(key, out ITutorialTarget target))
                {
                    resolvedTargets.Add(target);
                }
                else
                {
                    Echo.Warning($"Tutorial allowed target '{(key == null ? "<none>" : key.name)}' is not registered; it will not be interactable.");
                }
            }

            _gate.RestrictToAllowList(resolvedTargets);
            return UniTask.FromResult(true);
        }

        public UniTask DeactivateAsync(CancellationToken cancellationToken)
        {
            if (_gate != null)
            {
                _gate.ClearRestrictions();
            }

            return UniTask.CompletedTask;
        }
    }
}
