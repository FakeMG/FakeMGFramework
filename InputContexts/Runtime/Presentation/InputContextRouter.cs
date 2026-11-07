using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using VContainer.Unity;

namespace FakeMG.InputContexts
{
    /// <summary>
    /// Runs the owning context's handler whenever an owned action performs, so only the contexts on top respond, and a
    /// dialog's Back reaches the dialog rather than the screen beneath it. Each action is listened to once, from the first
    /// time any context handles it, and the owner is decided at the moment it performs.
    /// </summary>
    public sealed class InputContextRouter : IInitializable, IDisposable
    {
        private readonly InputContextStack _contextStack;
        private readonly HashSet<InputAction> _routedActions = new();

        #region Public Methods

        public InputContextRouter(InputContextStack contextStack)
        {
            _contextStack = contextStack;
        }

        public void Initialize()
        {
            _contextStack.OnOwnedActionsChanged += RouteNewlyOwnedActions;
            RouteNewlyOwnedActions();
        }

        public void Dispose()
        {
            _contextStack.OnOwnedActionsChanged -= RouteNewlyOwnedActions;
            foreach (InputAction action in _routedActions)
            {
                action.performed -= RunOwnerHandler;
            }

            _routedActions.Clear();
        }

        #endregion

        #region Private Methods

        /// <remarks>
        /// Never stops listening: an action no context handles simply finds no owner. One listener per action means one
        /// handler per press, so the press that closes a dialog never also reaches the screen revealed beneath it.
        /// </remarks>
        private void RouteNewlyOwnedActions()
        {
            foreach (IInputContext context in _contextStack.ActiveContexts)
            {
                foreach (InputAction action in context.Handlers.Actions)
                {
                    if (_routedActions.Add(action))
                    {
                        action.performed += RunOwnerHandler;
                    }
                }
            }
        }

        /// <remarks>
        /// Another <c>performed</c> callback may switch action maps first, which re-resolves bindings and leaves
        /// <see cref="InputAction.CallbackContext.control"/> pointing past the new control list. So the context is never read:
        /// an action that callback disabled has been handled already, and the control comes from the action's live state.
        /// </remarks>
        private void RunOwnerHandler(InputAction.CallbackContext callbackContext)
        {
            InputAction action = callbackContext.action;
            if (!action.enabled || IsTypingWith(action.activeControl))
            {
                return;
            }

            if (_contextStack.TryFindOwner(action, out IInputContext owner))
            {
                owner.Handlers.Handle(action);
            }
        }

        /// <summary>Action keys are also letters the player types, so a key pressed into a focused text field never acts.</summary>
        private static bool IsTypingWith(InputControl control)
        {
            if (control?.device is not Keyboard || !EventSystem.current)
            {
                return false;
            }

            GameObject selected = EventSystem.current.currentSelectedGameObject;
            return selected && selected.TryGetComponent(out TMP_InputField field) && field.isFocused;
        }

        #endregion
    }
}
