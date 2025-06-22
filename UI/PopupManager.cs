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

        private readonly Dictionary<PopupAnimator, Image> _popupDict = new();

        private void Start()
        {
            foreach (Transform child in transform)
            {
                if (child.TryGetComponent(out PopupAnimator popupAnimator))
                {
                    popupAnimator.onShowStart.AddListener(() => OnPopupOpen(popupAnimator));
                    popupAnimator.onHideStart.AddListener(() => HideBackground(popupAnimator));
                    popupAnimator.onHideFinished.AddListener(() => OnPopupClose(popupAnimator));
                }
                else
                {
                    Debug.LogError(
                        $"Child {child.name} does not have a PopupAnimator component. Please ensure all children of PopupManager have this component.");
                }
            }
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
            background.DOFade(BACKGROUND_FADE_ALPHA, BACKGROUND_FADE_DURATION);

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
                background.DOFade(0f, BACKGROUND_FADE_DURATION).OnComplete(() => Destroy(background.gameObject));
            }
        }

        private void OnPopupClose(PopupAnimator popupAnimator)
        {
            if (!_popupDict.Remove(popupAnimator))
            {
                Debug.LogWarning($"Popup {popupAnimator.name} is not open!");
            }

            // The popup is disabled, so we move it to the end of the hierarchy
            // to ensure it doesn't interfere with the opened popups.
            popupAnimator.transform.SetAsLastSibling();
        }
    }
}