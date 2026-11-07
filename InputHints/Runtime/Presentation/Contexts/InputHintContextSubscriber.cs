using System;
using FakeMG.Framework.EventBus;
using VContainer.Unity;

namespace FakeMG.InputHints
{
    /// <summary>Adds hint contexts to the active hints as they announce themselves, from any scene or popup.</summary>
    /// <remarks>Subscribes on initialization, while the persistent scope builds, so no later scene can announce too early.</remarks>
    public sealed class InputHintContextSubscriber : IInitializable, IDisposable
    {
        private readonly ActiveInputHints _activeHints;

        public InputHintContextSubscriber(ActiveInputHints activeHints)
        {
            _activeHints = activeHints;
        }

        #region Public Methods

        public void Initialize()
        {
            EventBus<AddInputHintContextEvent>.OnEvent += AddHintContext;
            EventBus<RemoveInputHintContextEvent>.OnEvent += RemoveHintContext;
        }

        public void Dispose()
        {
            EventBus<AddInputHintContextEvent>.OnEvent -= AddHintContext;
            EventBus<RemoveInputHintContextEvent>.OnEvent -= RemoveHintContext;
        }

        #endregion

        #region Private Methods

        private void AddHintContext(AddInputHintContextEvent evt)
        {
            _activeHints.Add(evt.HintContext);
        }

        private void RemoveHintContext(RemoveInputHintContextEvent evt)
        {
            _activeHints.Remove(evt.HintContext);
        }

        #endregion
    }
}
