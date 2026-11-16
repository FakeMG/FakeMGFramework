using System;
using UnityEngine;

namespace FakeMG.Tutorial
{
    public sealed class ConditionTutorialTarget : ITutorialTarget
    {
        private readonly Func<bool> _isAvailable;

        public ConditionTutorialTarget(TutorialTargetKeySO key, Func<bool> isAvailable)
        {
            Key = key;
            _isAvailable = isAvailable;
        }

        public TutorialTargetKeySO Key { get; }

        public bool IsAvailable => _isAvailable();

        public Transform InteractionTransform => null;
    }
}
