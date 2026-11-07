using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace FakeMG.InputContexts
{
    /// <summary>
    /// Decides which contexts are active from those on screen: the newest comes first, down to and including the newest modal
    /// one. An action belongs to the newest active context with a handler for it, so a dialog's Back never also triggers the
    /// Back of the screen beneath it.
    /// </summary>
    public sealed class InputContextStack
    {
        /// <remarks>Oldest first.</remarks>
        private readonly List<IInputContext> _contexts = new();
        private readonly List<IInputContext> _activeContexts = new();

        public event Action OnActiveContextsChanged;

        /// <summary>Raised when the active contexts change, or a shown context starts or stops handling an action.</summary>
        public event Action OnOwnedActionsChanged;

        /// <remarks>Newest first.</remarks>
        public IReadOnlyList<IInputContext> ActiveContexts => _activeContexts;

        #region Public Methods

        public void Show(IInputContext context)
        {
            if (_contexts.Contains(context))
            {
                return;
            }

            _contexts.Add(context);
            context.Handlers.OnChanged += ReportOwnedActionsChanged;
            RefreshActiveContexts();
        }

        public void Hide(IInputContext context)
        {
            if (!_contexts.Remove(context))
            {
                return;
            }

            context.Handlers.OnChanged -= ReportOwnedActionsChanged;
            RefreshActiveContexts();
        }

        public bool TryFindOwner(InputAction action, out IInputContext owner)
        {
            foreach (IInputContext context in _activeContexts)
            {
                if (context.Handlers.Handles(action))
                {
                    owner = context;
                    return true;
                }
            }

            owner = null;
            return false;
        }

        #endregion

        #region Private Methods

        private void RefreshActiveContexts()
        {
            _activeContexts.Clear();
            for (int i = _contexts.Count - 1; i >= 0; i--)
            {
                _activeContexts.Add(_contexts[i]);
                if (_contexts[i].IsModal)
                {
                    break;
                }
            }

            OnActiveContextsChanged?.Invoke();
            ReportOwnedActionsChanged();
        }

        private void ReportOwnedActionsChanged()
        {
            OnOwnedActionsChanged?.Invoke();
        }

        #endregion
    }
}
