using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FakeMG.InputHints
{
    [Serializable]
    public sealed class InputHint
    {
        [SerializeField] private string _label;
        [Tooltip("Shown side by side, e.g. Rotate Left and Rotate Right as one 'Rotate' hint.")]
        [SerializeField] private InputActionReference[] _actionReferences = Array.Empty<InputActionReference>();

        [NonSerialized] private bool _isShown = true;

        public event Action OnChanged;

        public string Label => _label;
        public bool IsShown => _isShown;
        public IReadOnlyList<InputActionReference> ActionReferences => _actionReferences;

        public InputHint(string label, params InputActionReference[] actionReferences)
        {
            _label = label;
            _actionReferences = actionReferences;
        }

        #region Public Methods

        public bool Uses(InputAction action)
        {
            foreach (InputActionReference actionReference in _actionReferences)
            {
                if (actionReference.action == action)
                {
                    return true;
                }
            }

            return false;
        }

        public void SetLabel(string label)
        {
            if (_label == label)
            {
                return;
            }

            _label = label;
            OnChanged?.Invoke();
        }

        public void SetShown(bool isShown)
        {
            if (_isShown == isShown)
            {
                return;
            }

            _isShown = isShown;
            OnChanged?.Invoke();
        }

        #endregion
    }
}
