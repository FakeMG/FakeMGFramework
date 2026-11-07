using System;
using System.Collections.Generic;
using FakeMG.InputContexts;
using UnityEngine.InputSystem;

namespace FakeMG.InputHints
{
    /// <summary>
    /// The hints to show now: those of the active input contexts, newest context first, so a modal dialog hides the hints
    /// of the screen beneath it. An action shows once, from the newest hint using it. Hidden hints neither show nor take
    /// their actions.
    /// </summary>
    public sealed class ActiveInputHints : IDisposable
    {
        private readonly InputContextStack _contextStack;
        /// <remarks>One hint context per input context.</remarks>
        private readonly Dictionary<IInputContext, IInputHintContext> _hintContextsByContext = new();
        private readonly List<InputHint> _hints = new();

        public event Action OnChanged;

        /// <remarks>Newest context first, each context's hints in authored order.</remarks>
        public IReadOnlyList<InputHint> Hints => _hints;

        public ActiveInputHints(InputContextStack contextStack)
        {
            _contextStack = contextStack;
            _contextStack.OnActiveContextsChanged += RefreshHints;
        }

        #region Public Methods

        public void Add(IInputHintContext hintContext)
        {
            if (!_hintContextsByContext.TryAdd(hintContext.InputContext, hintContext))
            {
                return;
            }

            foreach (InputHint hint in hintContext.Hints)
            {
                hint.OnChanged += RefreshHints;
            }

            RefreshHints();
        }

        public void Remove(IInputHintContext hintContext)
        {
            if (!_hintContextsByContext.Remove(hintContext.InputContext))
            {
                return;
            }

            foreach (InputHint hint in hintContext.Hints)
            {
                hint.OnChanged -= RefreshHints;
            }

            RefreshHints();
        }

        public void Dispose()
        {
            _contextStack.OnActiveContextsChanged -= RefreshHints;
            foreach (IInputHintContext hintContext in _hintContextsByContext.Values)
            {
                foreach (InputHint hint in hintContext.Hints)
                {
                    hint.OnChanged -= RefreshHints;
                }
            }

            _hintContextsByContext.Clear();
        }

        #endregion

        #region Private Methods

        private void RefreshHints()
        {
            _hints.Clear();
            foreach (IInputContext context in _contextStack.ActiveContexts)
            {
                if (!_hintContextsByContext.TryGetValue(context, out IInputHintContext hintContext))
                {
                    continue;
                }

                foreach (InputHint hint in hintContext.Hints)
                {
                    if (hint.IsShown && !IsAnyActionShownByNewerHint(hint))
                    {
                        _hints.Add(hint);
                    }
                }
            }

            OnChanged?.Invoke();
        }

        private bool IsAnyActionShownByNewerHint(InputHint hint)
        {
            foreach (InputHint shownHint in _hints)
            {
                foreach (InputActionReference actionReference in hint.ActionReferences)
                {
                    if (shownHint.Uses(actionReference.action))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion
    }
}
