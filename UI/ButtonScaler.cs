using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FakeMG.Framework.UI
{
    [RequireComponent(typeof(Button))]
    public class ButtonScaler : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private Transform _targetTransform;
        [SerializeField] private Button _button;
        [SerializeField] private float _selectScaleMultiplier = 1.2f;
        [SerializeField] private float _pressedShrinkAmount = 0.15f;
        [SerializeField] private float _animationDuration = 0.1f;

        private Vector3 _normalScale;

        public Transform TargetTransform
        {
            get => _targetTransform;
            set => _targetTransform = value;
        }

        private void Reset()
        {
            _targetTransform = transform;
            _button = GetComponent<Button>();
            _selectScaleMultiplier = 1.2f;
            _pressedShrinkAmount = 0.15f;
            _animationDuration = 0.1f;
        }

        private void Awake()
        {
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
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _button.Select();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_button.interactable) return;

            AnimateScale(_targetTransform.localScale - Vector3.one * _pressedShrinkAmount);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (EventSystem.current.currentSelectedGameObject == gameObject && _button.interactable)
            {
                AnimateScale(_normalScale * _selectScaleMultiplier);
            }
            else
            {
                AnimateScale(_normalScale);
            }
        }

        public void OnSelect(BaseEventData eventData)
        {
            AnimateScale(_normalScale * _selectScaleMultiplier);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            AnimateScale(_normalScale);
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