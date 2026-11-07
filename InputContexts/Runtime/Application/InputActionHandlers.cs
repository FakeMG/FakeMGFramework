using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace FakeMG.InputContexts
{
    /// <summary>The actions one context handles, and what each does. Adding a second handler for an action runs both.</summary>
    public sealed class InputActionHandlers
    {
        private readonly Dictionary<InputAction, Action> _handlersByAction = new();

        public event Action OnChanged;

        public IReadOnlyCollection<InputAction> Actions => _handlersByAction.Keys;

        #region Public Methods

        public void AddHandler(InputAction action, Action handler)
        {
            _handlersByAction[action] = _handlersByAction.TryGetValue(action, out Action existingHandler) ? existingHandler + handler : handler;
            OnChanged?.Invoke();
        }

        public void RemoveHandler(InputAction action, Action handler)
        {
            if (!_handlersByAction.TryGetValue(action, out Action existingHandler))
            {
                return;
            }

            Action remainingHandler = existingHandler - handler;
            if (remainingHandler == null)
            {
                _handlersByAction.Remove(action);
            }
            else
            {
                _handlersByAction[action] = remainingHandler;
            }

            OnChanged?.Invoke();
        }

        public bool Handles(InputAction action)
        {
            return _handlersByAction.ContainsKey(action);
        }

        /// <summary>Runs the action's handlers; does nothing for an action this context does not handle.</summary>
        public void Handle(InputAction action)
        {
            if (_handlersByAction.TryGetValue(action, out Action handler))
            {
                handler();
            }
        }

        #endregion
    }
}
