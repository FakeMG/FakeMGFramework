using System;
using FakeMG.Framework.EventBus;
using VContainer.Unity;

namespace FakeMG.InputContexts
{
    /// <summary>Puts contexts on the stack as they announce themselves, from any scene or popup.</summary>
    /// <remarks>Subscribes on initialization, while the persistent scope builds, so no later scene can announce too early.</remarks>
    public sealed class InputContextSubscriber : IInitializable, IDisposable
    {
        private readonly InputContextStack _contextStack;

        public InputContextSubscriber(InputContextStack contextStack)
        {
            _contextStack = contextStack;
        }

        #region Public Methods

        public void Initialize()
        {
            EventBus<ShowInputContextEvent>.OnEvent += ShowContext;
            EventBus<HideInputContextEvent>.OnEvent += HideContext;
        }

        public void Dispose()
        {
            EventBus<ShowInputContextEvent>.OnEvent -= ShowContext;
            EventBus<HideInputContextEvent>.OnEvent -= HideContext;
        }

        #endregion

        #region Private Methods

        private void ShowContext(ShowInputContextEvent evt)
        {
            _contextStack.Show(evt.Context);
        }

        private void HideContext(HideInputContextEvent evt)
        {
            _contextStack.Hide(evt.Context);
        }

        #endregion
    }
}
