using DG.Tweening;
using UnityEngine;

namespace FakeMG.Framework.UI.Tab.TabButton
{
    public class TabButtonSlide : TabButtonBase
    {
        [SerializeField] private RectTransform _buttonIconRect;
        [SerializeField] private float _buttonScaleMultiplier = 1.3f;
        [SerializeField] private float _buttonYOffset = 50f;
        [SerializeField] private float _animationDuration = 0.3f;
        [SerializeField] private Ease _scaleEase = Ease.OutQuart;
        [SerializeField] private Ease _moveYEase = Ease.OutBack;

        public override void AnimateSelection()
        {
            _buttonIconRect.DOKill();

            Vector3 targetScale = Vector3.one * _buttonScaleMultiplier;
            float targetY = _buttonYOffset;

            _buttonIconRect.DOScale(targetScale, _animationDuration).SetEase(_scaleEase)
                .SetLink(_buttonIconRect.gameObject);
            _buttonIconRect.DOAnchorPosY(targetY, _animationDuration).SetEase(_moveYEase)
                .SetLink(_buttonIconRect.gameObject);
        }

        public override void AnimateDeselection()
        {
            _buttonIconRect.DOKill();

            Vector3 targetScale = Vector3.one;
            float targetY = 0f;

            _buttonIconRect.DOScale(targetScale, _animationDuration).SetEase(_scaleEase)
                .SetLink(_buttonIconRect.gameObject);
            _buttonIconRect.DOAnchorPosY(targetY, _animationDuration).SetEase(_moveYEase)
                .SetLink(_buttonIconRect.gameObject);
        }

        public override void InstantlySelect()
        {
            _buttonIconRect.DOKill();

            _buttonIconRect.localScale = Vector3.one * _buttonScaleMultiplier;
            _buttonIconRect.anchoredPosition = new Vector2(_buttonIconRect.anchoredPosition.x, _buttonYOffset);
        }

        public override void InstantlyDeselect()
        {
            _buttonIconRect.DOKill();

            _buttonIconRect.localScale = Vector3.one;
            _buttonIconRect.anchoredPosition = new Vector2(_buttonIconRect.anchoredPosition.x, 0f);
        }
    }
}