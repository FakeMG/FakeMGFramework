using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FakeMG.Toast
{
    /// <summary>Default toast animation: scale + fade for show/hide, tweened reposition. Unscaled time.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ToastScaleFadeAnimator : ToastAnimator
    {
        [Required]
        [SerializeField] private CanvasGroup _canvasGroup;
        [Required]
        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private float _showDurationSeconds = 0.25f;
        [SerializeField] private float _hideDurationSeconds = 0.2f;
        [SerializeField] private float _moveDurationSeconds = 0.2f;
        [SerializeField] private Ease _showEase = Ease.OutBack;
        [SerializeField] private Ease _hideEase = Ease.InBack;
        [SerializeField] private Ease _moveEase = Ease.OutQuad;

        private Sequence _scaleFadeSequence;
        private Tween _moveTween;

        #region Lifecycle
        private void Reset()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnDestroy()
        {
            KillRunningTweens();
        }
        #endregion

        #region Public
        public override async UniTask ShowAsync(CancellationToken cancellationToken)
        {
            _scaleFadeSequence?.Kill();
            _rectTransform.localScale = Vector3.zero;
            _canvasGroup.alpha = 0f;

            _scaleFadeSequence = DOTween.Sequence()
                .Join(_rectTransform.DOScale(Vector3.one, _showDurationSeconds).SetEase(_showEase))
                .Join(_canvasGroup.DOFade(1f, _showDurationSeconds))
                .SetUpdate(true)
                .SetLink(gameObject);

            await AwaitTweenAsync(_scaleFadeSequence, cancellationToken);
        }

        public override async UniTask HideAsync(CancellationToken cancellationToken)
        {
            _scaleFadeSequence?.Kill();

            _scaleFadeSequence = DOTween.Sequence()
                .Join(_rectTransform.DOScale(Vector3.zero, _hideDurationSeconds).SetEase(_hideEase))
                .Join(_canvasGroup.DOFade(0f, _hideDurationSeconds))
                .SetUpdate(true)
                .SetLink(gameObject);

            await AwaitTweenAsync(_scaleFadeSequence, cancellationToken);
        }

        public override async UniTask MoveToAsync(Vector2 anchoredPositionPixels, CancellationToken cancellationToken)
        {
            // Repositions arrive in bursts; the previous move must die or the tweens fight.
            _moveTween?.Kill();

            _moveTween = _rectTransform.DOAnchorPos(anchoredPositionPixels, _moveDurationSeconds)
                .SetEase(_moveEase)
                .SetUpdate(true)
                .SetLink(gameObject);

            await AwaitTweenAsync(_moveTween, cancellationToken);
        }

        public override void SetVisualsToHiddenState()
        {
            KillRunningTweens();
            _canvasGroup.alpha = 0f;
            _rectTransform.localScale = Vector3.one;
        }
        #endregion

        #region Private
        private static async UniTask AwaitTweenAsync(Tween tween, CancellationToken cancellationToken)
        {
            await tween.AsyncWaitForCompletion().AsUniTask()
                .AttachExternalCancellation(cancellationToken)
                .SuppressCancellationThrow();
        }

        private void KillRunningTweens()
        {
            _scaleFadeSequence?.Kill();
            _moveTween?.Kill();
        }
        #endregion
    }
}
