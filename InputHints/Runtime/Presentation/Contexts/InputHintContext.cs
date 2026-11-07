using System;
using System.Collections.Generic;
using FakeMG.Framework.EventBus;
using FakeMG.InputContexts;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FakeMG.InputHints
{
    /// <summary>
    /// The hints of the <see cref="InputContext"/> on the same GameObject, shown while that context is active. Display-only:
    /// handle the actions through the input context.
    /// </summary>
    [RequireComponent(typeof(InputContext))]
    public sealed class InputHintContext : MonoBehaviour, IInputHintContext
    {
        [Tooltip("Hints shown while this context is active. Display-only: the input context still handles the actions.")]
        [SerializeField] private List<InputHint> _hints = new();

        private InputContext _inputContext;

        public IInputContext InputContext => _inputContext;
        public IReadOnlyList<InputHint> Hints => _hints;

        #region Unity Lifecycle

        private void Awake()
        {
            _inputContext = GetComponent<InputContext>();
        }

        private void OnEnable()
        {
            EventBus<AddInputHintContextEvent>.Raise(new AddInputHintContextEvent { HintContext = this });
        }

        private void OnDisable()
        {
            EventBus<RemoveInputHintContextEvent>.Raise(new RemoveInputHintContextEvent { HintContext = this });
        }

        #endregion

        #region Public Methods

        /// <exception cref="ArgumentException">No hint uses the action: an authoring mistake to fix in the Inspector.</exception>
        public InputHint GetHintFor(InputActionReference actionReference)
        {
            foreach (InputHint hint in _hints)
            {
                if (hint.Uses(actionReference.action))
                {
                    return hint;
                }
            }

            throw new ArgumentException($"Input hint context '{name}' has no hint for '{actionReference.name}'.", nameof(actionReference));
        }

        #endregion
    }
}
