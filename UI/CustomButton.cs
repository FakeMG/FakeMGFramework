using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FakeMG.FakeMGFramework.UI
{
    public class CustomButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler,
        IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Color normalColor = new(1f, 1f, 1f, 1f);
        [SerializeField] private Color hoverColor = new(0.7f, 0.7f, 0.7f, 1f);
        [SerializeField] private Color disabledColor = new(0.4f, 0.4f, 0.4f, 1f);
        [SerializeField] private Transform visual;
        [SerializeField] private ScrollRect scrollRect;
        public UnityEvent onClick;

        private RectTransform _rectTransform;
        private List<Image> _images;
        private List<TextMeshProUGUI> _textMeshProUGUIs;
        private Vector3 _normalScale;

        private const float HOVER_SCALE_MULTIPLIER = 0.95f;
        private const float PRESS_SCALE_MULTIPLIER = 0.85f;
        private const float ANIMATION_DURATION = 0.1f;

        private bool _isDisabled;
        private bool _isDragging;

        private enum State
        {
            Normal,
            Hover,
            Pressed,
            Disabled
        }

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();

            _images = GetComponentsInChildren<Image>(true).Where(img => img.gameObject != gameObject).ToList();
            _textMeshProUGUIs = GetComponentsInChildren<TextMeshProUGUI>(true).Where(text => text.gameObject != gameObject).ToList();

            _normalScale = visual.transform.localScale;
            if (_normalScale == Vector3.zero)
            {
                _normalScale = Vector3.one; // Default to one if scale is zero
            }
        }

        private void OnEnable()
        {
            if (_isDisabled) ChangeState(State.Disabled);
        }

        public void DisableButton()
        {
            _isDisabled = true;
            ChangeColor(disabledColor);
        }

        public void DisableButtonWithoutColorChange()
        {
            _isDisabled = true;
        }

        public void EnableButton()
        {
            _isDisabled = false;
            ChangeColor(normalColor);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isDisabled) return;
            ChangeState(State.Hover);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_isDisabled) return;
            ChangeState(State.Normal);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_isDisabled) return;
            ChangeState(State.Pressed);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_isDisabled) return;

            ChangeState(State.Normal);

            //prevent multi touch
            if (Input.touchCount > 1) return;

            //check if the button is still under the pointer using eventData
            if (!RectTransformUtility.RectangleContainsScreenPoint(_rectTransform, eventData.position)) return;
            if (_isDragging) return;

            onClick.Invoke();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (scrollRect)
            {
                scrollRect.OnDrag(eventData);
            }

            _isDragging = true;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (scrollRect)
            {
                scrollRect.OnBeginDrag(eventData);
            }

            _isDragging = true;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (scrollRect)
            {
                scrollRect.OnEndDrag(eventData);
            }

            _isDragging = false;
        }

        private void ChangeColor(Color color)
        {
            if (_images == null) return;
            foreach (var image in _images)
            {
                image.CrossFadeColor(color, ANIMATION_DURATION, true, true);
            }

            if (_textMeshProUGUIs == null) return;
            foreach (var text in _textMeshProUGUIs)
            {
                text.CrossFadeColor(color, ANIMATION_DURATION, true, true);
            }
        }

        private void ChangeState(State buttonState)
        {
            switch (buttonState)
            {
                case State.Normal:
                    visual?.transform.DOScale(_normalScale, ANIMATION_DURATION).SetEase(Ease.InOutQuad).SetUpdate(true)
                            .SetLink(gameObject);
                    ChangeColor(normalColor);
                    break;
                case State.Hover:
                    visual?.transform.DOScale(_normalScale * HOVER_SCALE_MULTIPLIER, ANIMATION_DURATION).SetEase(Ease.InOutQuad).SetUpdate(true)
                            .SetLink(gameObject);
                    ChangeColor(hoverColor);
                    break;
                case State.Pressed:
                    visual?.transform.DOScale(_normalScale * PRESS_SCALE_MULTIPLIER, ANIMATION_DURATION).SetEase(Ease.InOutQuad).SetUpdate(true)
                            .SetLink(gameObject);
                    ChangeColor(hoverColor);
                    break;
                case State.Disabled:
                    ChangeColor(disabledColor);
                    break;
            }
        }
    }
}