using System;
using FakeMG.Framework.EventBus;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FakeMG.InputContexts
{
    /// <summary>
    /// One screen, popup or gameplay mode that handles input. Put it on the object that is active while it applies, and it
    /// is shown while enabled; or turn off <c>Shown While Enabled</c> and call <see cref="Show"/> and <see cref="Hide"/> for a
    /// mode that code switches on and off. Its owner handles actions with <see cref="AddHandler"/>, and only while this context is
    /// the newest active one handling that action.
    /// </summary>
    public sealed class InputContext : MonoBehaviour, IInputContext
    {
        [Tooltip("Silences every context shown before this one, e.g. a dialog over its screen.")]
        [SerializeField] private bool _isModal;
        [Tooltip("Off for contexts that code shows and hides, such as a gameplay mode.")]
        [SerializeField] private bool _isShownWhileEnabled = true;

        private readonly InputActionHandlers _handlers = new();
        private bool _isShown;

        public bool IsModal => _isModal;
        public InputActionHandlers Handlers => _handlers;

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (_isShownWhileEnabled)
            {
                Show();
            }
        }

        private void OnDisable()
        {
            Hide();
        }

        #endregion

        #region Public Methods

        public void Show()
        {
            if (_isShown)
            {
                return;
            }

            _isShown = true;
            EventBus<ShowInputContextEvent>.Raise(new ShowInputContextEvent { Context = this });
        }

        public void Hide()
        {
            if (!_isShown)
            {
                return;
            }

            _isShown = false;
            EventBus<HideInputContextEvent>.Raise(new HideInputContextEvent { Context = this });
        }

        /// <summary>Handles the action with <paramref name="handler"/> while this context owns it. Pair with <see cref="RemoveHandler"/>.</summary>
        public void AddHandler(InputActionReference actionReference, Action handler)
        {
            _handlers.AddHandler(actionReference.action, handler);
        }

        public void RemoveHandler(InputActionReference actionReference, Action handler)
        {
            _handlers.RemoveHandler(actionReference.action, handler);
        }

        #endregion
    }
}
