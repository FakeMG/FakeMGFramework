using FakeMG.Framework;
using System;
using System.Collections.Generic;
using FakeMG.Framework.EventBus;
using VContainer.Unity;

namespace FakeMG.Tutorial
{
    public struct TutorialTargetRegisteredEvent : IEvent
    {
        public ITutorialTarget Target;
    }

    public struct TutorialTargetUnregisteredEvent : IEvent
    {
        public ITutorialTarget Target;
    }

    /// <summary>
    /// Resolves tutorial targets by their key asset. Targets self-register through the
    /// event bus when they become available and unregister when they go away, so both
    /// scene targets and runtime-instantiated ones (e.g. a button inside a popup created
    /// by PopupManager) are found without serialized references. Registered as an entry
    /// point so it subscribes during scope build, before scene targets raise enable events.
    /// </summary>
    public sealed class TutorialTargetRegistry : IStartable, IDisposable
    {
        private readonly Dictionary<TutorialTargetKeySO, ITutorialTarget> _targetsByKey = new();

        public TutorialTargetRegistry()
        {
            EventBus<TutorialTargetRegisteredEvent>.OnEvent += RegisterTarget;
            EventBus<TutorialTargetUnregisteredEvent>.OnEvent += UnregisterTarget;
        }

        public void Start()
        {
            // Intentionally empty.
            // Implemented so VContainer creates this entry point during scope startup.
            // Event subscriptions happen in the constructor.
        }

        public void Dispose()
        {
            EventBus<TutorialTargetRegisteredEvent>.OnEvent -= RegisterTarget;
            EventBus<TutorialTargetUnregisteredEvent>.OnEvent -= UnregisterTarget;
        }

        public bool TryGet(TutorialTargetKeySO key, out ITutorialTarget target)
        {
            if (key == null)
            {
                target = null;
                return false;
            }

            return _targetsByKey.TryGetValue(key, out target);
        }

        private void RegisterTarget(TutorialTargetRegisteredEvent registeredEvent)
        {
            ITutorialTarget target = registeredEvent.Target;
            if (target.Key == null)
            {
                Echo.Warning("A tutorial target registered without a key asset and was ignored.");
                return;
            }

            _targetsByKey[target.Key] = target;
        }

        private void UnregisterTarget(TutorialTargetUnregisteredEvent unregisteredEvent)
        {
            ITutorialTarget target = unregisteredEvent.Target;
            if (target.Key != null && _targetsByKey.TryGetValue(target.Key, out ITutorialTarget existing) && existing == target)
            {
                _targetsByKey.Remove(target.Key);
            }
        }
    }
}
