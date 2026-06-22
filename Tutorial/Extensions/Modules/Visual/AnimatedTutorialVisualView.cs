using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Fade-and-scale visual view usable directly for simple visuals or as a base for
    /// content-specific views (text box, pointer, framing). Animates via DOTween so
    /// nothing appears or disappears instantly.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class AnimatedTutorialVisualView : TutorialVisualView
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _showDurationSeconds = 0.25f;
        [SerializeField] private float _hideDurationSeconds = 0.2f;
        [SerializeField] private Vector3 _hiddenScale = new(0.85f, 0.85f, 0.85f);
        [SerializeField] private Vector3 _shownScale = Vector3.one;

        private void Reset()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        public override async UniTask ShowAsync(CancellationToken cancellationToken)
        {
            gameObject.SetActive(true);
            _canvasGroup.alpha = 0f;
            transform.localScale = _hiddenScale;

            Sequence sequence = DOTween.Sequence();
            sequence.Join(_canvasGroup.DOFade(1f, _showDurationSeconds));
            sequence.Join(transform.DOScale(_shownScale, _showDurationSeconds));

            await sequence.ToUniTask(cancellationToken: cancellationToken);
        }

        public override async UniTask HideAsync(CancellationToken cancellationToken)
        {
            Sequence sequence = DOTween.Sequence();
            sequence.Join(_canvasGroup.DOFade(0f, _hideDurationSeconds));
            sequence.Join(transform.DOScale(_hiddenScale, _hideDurationSeconds));

            await sequence.ToUniTask(cancellationToken: cancellationToken);

            gameObject.SetActive(false);
        }

        /// <summary>
        /// Clamps an overlay-canvas screen position so the element stays fully on screen.
        /// </summary>
        protected static Vector3 ClampToScreen(Vector3 screenPosition, Vector2 paddingPixels)
        {
            screenPosition.x = Mathf.Clamp(screenPosition.x, paddingPixels.x, Screen.width - paddingPixels.x);
            screenPosition.y = Mathf.Clamp(screenPosition.y, paddingPixels.y, Screen.height - paddingPixels.y);
            return screenPosition;
        }
    }
}
