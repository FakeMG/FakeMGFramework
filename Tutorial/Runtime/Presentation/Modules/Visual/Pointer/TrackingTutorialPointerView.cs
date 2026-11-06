using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Hand pointer that follows an <see cref="ITutorialPointerAnchor"/> every frame, so it stays
    /// on targets that move or animate (popup buttons scaling in, world points while the camera
    /// turns). It fades out while the anchor is hidden and back in when it reappears, flips to stay
    /// on screen near edges, and loops a tap animation with a ripple at the exact target point.
    /// The hand image's pivot must sit on the fingertip.
    /// </summary>
    public sealed class TrackingTutorialPointerView : AnimatedTutorialVisualView
    {
        [Header("Hierarchy")]
        [Tooltip("Fades while the anchor is hidden, independent of the show/hide animation.")]
        [SerializeField] private CanvasGroup _trackingGroup;
        [SerializeField] private RectTransform _flipRoot;
        [SerializeField] private RectTransform _tapRoot;
        [SerializeField] private RectTransform _handRect;
        [Tooltip("Optional ring that expands from the target point on every tap.")]
        [SerializeField] private RectTransform _rippleRect;
        [SerializeField] private CanvasGroup _rippleGroup;

        [Header("Motion")]
        [SerializeField, Min(0.05f)] private float _tapDurationSeconds = 0.45f;
        [SerializeField, Range(0.5f, 1f)] private float _tapPressedScale01 = 0.82f;
        [SerializeField, Min(0.01f)] private float _trackingFadeDurationSeconds = 0.15f;
        [SerializeField, Min(0f)] private float _followSharpnessPerSecond = 30f;

        private ITutorialPointerAnchor _anchor;
        private Sequence _tapSequence;
        private RectTransform _parentRect;
        private Canvas _canvas;
        private bool _hasPlacedOnce;

        #region Unity Lifecycle

        private void Awake()
        {
            _parentRect = transform.parent as RectTransform;
            _canvas = GetComponentInParent<Canvas>();
            _trackingGroup.alpha = 0f;
        }

        private void LateUpdate()
        {
            bool isVisible = _anchor != null && _anchor.TryGetScreenPositionPixels(out Vector2 screenPositionPixels) &&
                             TryMoveTo(screenPositionPixels);
            float targetAlpha01 = isVisible ? 1f : 0f;
            _trackingGroup.alpha = Mathf.MoveTowards(_trackingGroup.alpha, targetAlpha01,
                Time.unscaledDeltaTime / _trackingFadeDurationSeconds);
            if (_trackingGroup.alpha == 0f)
            {
                _hasPlacedOnce = false;
            }
        }

        private void OnDestroy()
        {
            _tapSequence?.Kill();
        }

        #endregion

        #region Public Methods

        public void Track(ITutorialPointerAnchor anchor)
        {
            _anchor = anchor;
            _hasPlacedOnce = false;
        }

        public override async UniTask ShowAsync(CancellationToken cancellationToken)
        {
            StartTapLoop();
            await base.ShowAsync(cancellationToken);
        }

        public override async UniTask HideAsync(CancellationToken cancellationToken)
        {
            await base.HideAsync(cancellationToken);
            _tapSequence?.Kill();
            _tapSequence = null;
        }

        #endregion

        #region Private Methods

        private bool TryMoveTo(Vector2 screenPositionPixels)
        {
            Camera canvasCamera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRect, screenPositionPixels, canvasCamera,
                    out Vector2 localPosition))
            {
                return false;
            }

            // Place the hand immediately when it first appears or fades back in from fully hidden.
            float follow01 = _hasPlacedOnce ? 1f - Mathf.Exp(-_followSharpnessPerSecond * Time.unscaledDeltaTime) : 1f;
            transform.localPosition = Vector3.Lerp(transform.localPosition, localPosition, follow01);
            _hasPlacedOnce = true;
            ApplyFlip(localPosition);
            return true;
        }

        private void ApplyFlip(Vector2 localPosition)
        {
            Rect parentRect = _parentRect.rect;
            Vector2 handSize = _handRect.rect.size;
            // The hand hangs below-right of the fingertip; flip it when it would leave the screen.
            bool shouldFlipHorizontally = localPosition.x + handSize.x > parentRect.xMax;
            bool shouldFlipVertically = localPosition.y - handSize.y < parentRect.yMin;
            _flipRoot.localScale = new Vector3(shouldFlipHorizontally ? -1f : 1f, shouldFlipVertically ? -1f : 1f, 1f);
        }

        private void StartTapLoop()
        {
            _tapSequence?.Kill();
            _tapRoot.localScale = Vector3.one;
            float halfSeconds = _tapDurationSeconds * 0.5f;
            _tapSequence = DOTween.Sequence()
                .Append(_tapRoot.DOScale(_tapPressedScale01, halfSeconds).SetEase(Ease.InOutSine))
                .Append(_tapRoot.DOScale(1f, halfSeconds).SetEase(Ease.OutBack));
            if (_rippleRect != null)
            {
                _tapSequence.InsertCallback(halfSeconds, ResetRipple);
                _tapSequence.Insert(halfSeconds, _rippleRect.DOScale(1.6f, _tapDurationSeconds).SetEase(Ease.OutQuad));
                _tapSequence.Insert(halfSeconds, _rippleGroup.DOFade(0f, _tapDurationSeconds).SetEase(Ease.OutQuad));
            }

            _tapSequence.AppendInterval(0.25f).SetLoops(-1).SetUpdate(true).SetLink(gameObject);
        }

        private void ResetRipple()
        {
            _rippleRect.localScale = Vector3.one * 0.3f;
            _rippleGroup.alpha = 0.8f;
        }

        #endregion
    }
}
