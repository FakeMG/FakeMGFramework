using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FakeMG.Framework.UI
{
    [RequireComponent(typeof(Button))]
    public class ScaleButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private Transform _targetTransform;
        [SerializeField] private float _hoverScaleMultiplier = 0.95f;
        [SerializeField] private float _pressScaleMultiplier = 0.85f;
        [SerializeField] private float _animationDuration = 0.1f;

        private Button _button;
        private Vector3 _normalScale;
        private bool _isPointerInside;
        private bool _isPressed;

        private void Awake()
        {
            _button = GetComponent<Button>();

            if (_targetTransform == null)
            {
                _targetTransform = transform;
            }

            _normalScale = _targetTransform.localScale;
            if (_normalScale == Vector3.zero)
            {
                _normalScale = Vector3.one;
            }
        }

        private void OnDisable()
        {
            _targetTransform.localScale = _normalScale;
            _isPointerInside = false;
            _isPressed = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_button.interactable) return;

            _isPointerInside = true;
            if (!_isPressed)
            {
                AnimateScale(_normalScale * _hoverScaleMultiplier);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isPointerInside = false;
            if (!_isPressed)
            {
                AnimateScale(_normalScale);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_button.interactable) return;

            _isPressed = true;
            AnimateScale(_normalScale * _pressScaleMultiplier);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressed = false;

            if (_isPointerInside && _button.interactable)
            {
                AnimateScale(_normalScale * _hoverScaleMultiplier);
            }
            else
            {
                AnimateScale(_normalScale);
            }
        }

        private void AnimateScale(Vector3 targetScale)
        {
            if (!gameObject.activeInHierarchy) return;

            _targetTransform.DOScale(targetScale, _animationDuration)
                .SetEase(Ease.InOutQuad)
                .SetUpdate(true)
                .SetLink(gameObject);
        }
    }
}