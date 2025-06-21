using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Sirenix.OdinInspector;

namespace FakeMG.FakeMGFramework.UI
{
    /// <summary>
    /// Manages the display and layering of popup windows with animated black backgrounds.
    /// Handles popup stacking, background creation/destruction, and fade animations.
    /// <para></para>
    /// IMPORTANT: All popup GameObjects must be direct children of this PopupManager and have a PopupAnimator component.
    /// </summary>
    public class PopupManager : MonoBehaviour
    {
        private const float BACKGROUND_FADE_ALPHA = 0.8f;
        private const float BACKGROUND_FADE_DURATION = 0.3f;
        
        [Required]
        [SerializeField] private Image blackBackgroundPrefab;

        private readonly Stack<PopupAnimator> _popupStack = new();
        private readonly Stack<Image> _backgroundStack = new();

        private void Start()
        {
            foreach (Transform child in transform)
            {
                if (child.TryGetComponent(out PopupAnimator popupAnimator))
                {
                    popupAnimator.onShowStart.AddListener(ShowBackground);
                    popupAnimator.onHideStart.AddListener(HideBackground);

                    popupAnimator.onShowStart.AddListener(() => OnPopupOpen(popupAnimator));
                    popupAnimator.onHideFinished.AddListener(() => OnPopupClose(popupAnimator));
                }
                else
                {
                    Debug.LogError($"Child {child.name} does not have a PopupAnimator component. Please ensure all children of PopupManager have this component.");
                }
            }
        }

        public void OnPopupOpen(PopupAnimator popupAnimator)
        {
            if (_popupStack.Contains(popupAnimator))
            {
                Debug.LogWarning($"Popup {popupAnimator.name} is already in the stack!");
                return;
            }

            popupAnimator.transform.SetParent(transform);
            popupAnimator.transform.SetSiblingIndex(_popupStack.Count * 2 + 1);
            _popupStack.Push(popupAnimator);
        }
        
        public void OnPopupClose(PopupAnimator popupAnimator)
        {
            if (_popupStack.Count > 0)
            {
                PopupAnimator topPopup = _popupStack.Peek();
                if (topPopup != popupAnimator)
                {
                    Debug.LogWarning($"Attempted to close {popupAnimator.name} but the top popup is {topPopup.name}. Only the top popup can be closed.");
                    return;
                }
                _popupStack.Pop();
                popupAnimator.transform.SetAsLastSibling();
            }
            else
            {
                Debug.LogWarning("Attempted to close a popup but the popup stack is empty.");
            }
        }

        public void ShowBackground()
        {
            Image background = Instantiate(blackBackgroundPrefab, transform);
            background.gameObject.SetActive(true);
            background.transform.SetSiblingIndex(_backgroundStack.Count * 2);
            _backgroundStack.Push(background);

            Color backgroundColor = background.color;
            backgroundColor.a = 0f;
            background.color = backgroundColor;
            background.DOFade(BACKGROUND_FADE_ALPHA, BACKGROUND_FADE_DURATION);
        }

        public void HideBackground()
        {
            if (_backgroundStack.Count > 0)
            {
                Image background = _backgroundStack.Pop();
                if (background)
                {
                    background.DOFade(0f, BACKGROUND_FADE_DURATION).OnComplete(() => Destroy(background.gameObject));
                }
            }
            else
            {
                Debug.LogWarning("Attempted to hide background but the background stack is empty.");
            }
        }
    }
}