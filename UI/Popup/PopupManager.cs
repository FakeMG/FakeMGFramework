using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using System;
using UnityEngine.UI;

namespace FakeMG.FakeMGFramework.UI.Popup
{
    /// <summary>
    /// Handles popup stacking, background creation/destruction.
    /// <para></para>
    /// IMPORTANT: All popup GameObjects must be direct children of this PopupManager and have a PopupAnimator component.
    /// </summary>
    public class PopupManager : MonoBehaviour
    {
        private const float BACKGROUND_FADE_DURATION = 0.3f;
        private float _backgroundFadeAlpha = 0.95f;

        [Required]
        [SerializeField] private Image blackBackgroundPrefab;

        public event Action OnShowStart;
        public event Action OnShowFinished;
        public event Action OnHideStart;
        public event Action OnHideFinished;

        private readonly Dictionary<PopupAnimator, Image> _popupDict = new();
        private readonly
            Dictionary<PopupAnimator, (Action onShowStart, Action onShowFinished, Action onHideStart, Action
                onHideFinished)> _eventDelegates = new();

        private void Start()
        {
            _backgroundFadeAlpha = blackBackgroundPrefab.color.a;

            var animators = GetComponentsInChildren<PopupAnimator>(true);

            foreach (var animator in animators)
            {
                animator.Hide(false); // Ensure the popup is hidden initially

                // capture the loop variable for safe closure
                var a = animator;

                // Create combined delegates for storage
                Action onShowStartCombined = OnShowStartHandler + new Action(OnShowStartForwarder);
                Action onShowFinishedCombined = OnShowFinishedHandler;
                Action onHideStartCombined = OnHideStartHandler + new Action(OnHideStartForwarder);
                Action onHideFinishedCombined = OnHideFinishedHandler + new Action(OnHideFinishedForwarder);

                // Store delegates for proper unsubscription
                _eventDelegates[a] = (
                    onShowStartCombined,
                    onShowFinishedCombined,
                    onHideStartCombined,
                    onHideFinishedCombined
                );

                // Subscribe to popup animator events
                a.OnShowStart += _eventDelegates[a].onShowStart;
                a.OnShowFinished += _eventDelegates[a].onShowFinished;
                a.OnHideStart += _eventDelegates[a].onHideStart;
                a.OnHideFinished += _eventDelegates[a].onHideFinished;
                continue;

                void OnShowStartHandler() => OnPopupOpen(a);
                void OnShowStartForwarder() => OnShowStart?.Invoke();
                void OnShowFinishedHandler() => OnShowFinished?.Invoke();

                void OnHideStartHandler() => HideBackground(a);
                void OnHideStartForwarder() => OnHideStart?.Invoke();
                void OnHideFinishedHandler() => OnPopupFinishClosing(a);
                void OnHideFinishedForwarder() => OnHideFinished?.Invoke();
            }
        }

        private void OnDestroy()
        {
            foreach (var kvp in _eventDelegates)
            {
                var animator = kvp.Key;
                var delegates = kvp.Value;

                if (animator != null)
                {
                    animator.OnShowStart -= delegates.onShowStart;
                    animator.OnShowFinished -= delegates.onShowFinished;
                    animator.OnHideStart -= delegates.onHideStart;
                    animator.OnHideFinished -= delegates.onHideFinished;
                }
            }

            _eventDelegates.Clear();
        }

        private void OnPopupOpen(PopupAnimator popupAnimator)
        {
            if (_popupDict.ContainsKey(popupAnimator))
            {
                Debug.LogWarning($"Popup {popupAnimator.name} is already open!");
                return;
            }

            // Show the new popup on top of other popups
            int topIndex = _popupDict.Count > 0 ? _popupDict.Count * 2 : 0;

            var background = ShowBackground();
            background.transform.SetSiblingIndex(topIndex);

            popupAnimator.transform.SetParent(transform);
            popupAnimator.transform.SetSiblingIndex(topIndex + 1);

            _popupDict.Add(popupAnimator, background);
        }

        private Image ShowBackground()
        {
            Image background = Instantiate(blackBackgroundPrefab, transform);
            background.gameObject.SetActive(true);

            Color backgroundColor = background.color;
            backgroundColor.a = 0f;
            background.color = backgroundColor;
            background.DOFade(_backgroundFadeAlpha, BACKGROUND_FADE_DURATION).SetLink(background.gameObject);

            return background;
        }

        private void HideBackground(PopupAnimator popupAnimator)
        {
            if (!_popupDict.TryGetValue(popupAnimator, out var background))
            {
                Debug.LogWarning($"Popup {popupAnimator.name} does not have an associated background to hide.");
                return;
            }

            if (background)
            {
                background.DOFade(0f, BACKGROUND_FADE_DURATION).SetLink(background.gameObject);
            }
        }

        private void OnPopupFinishClosing(PopupAnimator popupAnimator)
        {
            if (_popupDict.TryGetValue(popupAnimator, out var background))
            {
                Destroy(background.gameObject);
            }

            if (!_popupDict.Remove(popupAnimator))
            {
                Debug.LogWarning($"Popup {popupAnimator.name} is not in the popup dictionary!");
            }

            // The popup is disabled, so we move it to the end of the hierarchy
            // to ensure it doesn't interfere with the opened popups.
            popupAnimator.transform.SetAsLastSibling();
        }
    }
}