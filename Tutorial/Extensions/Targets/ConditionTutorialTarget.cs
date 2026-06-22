using System;
using UnityEngine;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// A target backed by an abstract condition rather than a visible object. Useful when
    /// a step gates on a state that has no on-screen element to point at.
    /// </summary>
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
