using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FakeMG.FakeMGFramework.UI
{
    public class ScaleButton : Button
    {
        private List<Graphic> _graphics;
        private List<TextMeshProUGUI> _textMeshPro;
        private Vector3 _normalScale;

        private const float HOVER_SCALE_MULTIPLIER = 0.95f;
        private const float PRESS_SCALE_MULTIPLIER = 0.85f;
        private const float ANIMATION_DURATION = 0.1f;

        protected override void Awake()
        {
            base.Awake();
            
            _graphics = GetComponentsInChildren<Graphic>(true).Where(graphic => graphic.gameObject != gameObject).ToList();
            _textMeshPro = GetComponentsInChildren<TextMeshProUGUI>(true)
                .Where(text => text.gameObject != gameObject).ToList();

            _normalScale = targetGraphic.transform.localScale;
            if (_normalScale == Vector3.zero)
            {
                _normalScale = Vector3.one;
            }
        }
        
        private void ChangeColor(Color targetColor, bool instant)
        {
            if (_graphics == null) return;
            foreach (var graphic in _graphics)
            {
                graphic.CrossFadeColor(targetColor, instant ? 0f : colors.fadeDuration, true, true);
            }

            if (_textMeshPro == null) return;
            foreach (var text in _textMeshPro)
            {
                text.CrossFadeColor(targetColor, instant ? 0f : colors.fadeDuration, true, true);
            }
        }

        protected override void DoStateTransition(SelectionState buttonState, bool instant)
        {
            if (!gameObject.activeInHierarchy)
                return;
            
            Color tintColor;
            
            switch (buttonState)
            {
                case SelectionState.Normal:
                    targetGraphic?.transform.DOScale(_normalScale, ANIMATION_DURATION).SetEase(Ease.InOutQuad).SetUpdate(true)
                        .SetLink(gameObject);
                    tintColor = colors.normalColor;
                    break;
                case SelectionState.Highlighted:
                    targetGraphic?.transform.DOScale(_normalScale * HOVER_SCALE_MULTIPLIER, ANIMATION_DURATION)
                        .SetEase(Ease.InOutQuad).SetUpdate(true)
                        .SetLink(gameObject);
                    tintColor = colors.highlightedColor;
                    break;
                case SelectionState.Pressed:
                    targetGraphic?.transform.DOScale(_normalScale * PRESS_SCALE_MULTIPLIER, ANIMATION_DURATION)
                        .SetEase(Ease.InOutQuad).SetUpdate(true)
                        .SetLink(gameObject);
                    tintColor = colors.pressedColor;
                    break;
                case SelectionState.Selected:
                    targetGraphic?.transform.DOScale(_normalScale, ANIMATION_DURATION)
                        .SetEase(Ease.InOutQuad).SetUpdate(true)
                        .SetLink(gameObject);
                    tintColor = colors.selectedColor;
                    break;
                case SelectionState.Disabled:
                    tintColor = colors.disabledColor;
                    break;
                default:
                    tintColor = Color.black;
                    break;
            }
                    
            ChangeColor(tintColor * colors.colorMultiplier, instant);
        }
    }
}