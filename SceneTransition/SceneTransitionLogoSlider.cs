using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FakeMG.Audio;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace FakeMG.SceneTransition
{
    public class SceneTransitionLogoSlider : MonoBehaviour, ISceneTransition
    {
        [Header("References")]
        [SerializeField] private CanvasGroup _transitionCanvasGroup;
        [SerializeField] private RectTransform _logoRectTransform;
        [SerializeField] private RectTransform _loadingSliderRectTransform;
        [SerializeField] private Slider _loadingProgressSlider;

        [Header("Show Animation")]
        [SerializeField] private float _fadeInDurationSeconds = 0.25f;
        [SerializeField] private float _moveDurationSeconds = 0.4f;
        [SerializeField] private float _fakeLoadingDurationSeconds = 1f;
        [SerializeField] private Vector2 _logoHiddenOffsetPixels = new(0f, 220f);
        [SerializeField] private Vector2 _loadingSliderHiddenOffsetPixels = new(0f, -220f);
        [SerializeField] private Ease _fadeInEase = Ease.OutQuad;
        [SerializeField] private Ease _moveEase = Ease.OutBack;
        [SerializeField] private Ease _fakeLoadingEase = Ease.Linear;

        [Header("Hide Animation")]
        [SerializeField] private float _fadeOutDurationSeconds = 0.2f;
        [SerializeField] private Ease _fadeOutEase = Ease.InQuad;

        private Vector2 _logoShownAnchoredPositionPixels;
        private Vector2 _loadingSliderShownAnchoredPositionPixels;
        private bool _isTransitionRunning;

        #region Unity Lifecycle

        private void Awake()
        {
            CacheShownLayout();
            ApplyHiddenLayout();
            _loadingProgressSlider.normalizedValue = 0f;
            _transitionCanvasGroup.alpha = 0f;
            _transitionCanvasGroup.gameObject.SetActive(false);
        }

#if UNITY_EDITOR
        [Button]
        private void Show()
        {
            ShowAsync().Forget();
        }

        [Button]
        private void Hide()
        {
            HideAsync().Forget();
        }
#endif

        #endregion

        #region Public Methods

        public UniTask PlayTransitionAsync(Action process)
        {
            return PlayTransitionAsync(() =>
            {
                process?.Invoke();
                return UniTask.CompletedTask;
            });
        }

        public async UniTask PlayTransitionAsync(Func<UniTask> processTask)
        {
            if (_isTransitionRunning)
            {
                Debug.LogWarning($"{nameof(SceneTransitionLogoSlider)} ignored a transition request because another transition is already running.", this);
                return;
            }

            _isTransitionRunning = true;
            bool hasShownTransition = false;

            try
            {
                await ShowAsync();
                hasShownTransition = true;

                if (processTask != null)
                {
                    await processTask();
                }
            }
            finally
            {
                try
                {
                    if (hasShownTransition)
                    {
                        await HideAsync();
                    }
                }
                finally
                {
                    _isTransitionRunning = false;
                }
            }
        }

        #endregion

        #region Private Methods

        private async UniTask ShowAsync()
        {
            KillActiveTweens();
            ApplyHiddenLayout();

            _transitionCanvasGroup.gameObject.SetActive(true);
            _transitionCanvasGroup.alpha = 0f;
            _loadingProgressSlider.normalizedValue = 0f;

            Tween fadeTween = _transitionCanvasGroup
                .DOFade(1f, _fadeInDurationSeconds)
                .SetEase(_fadeInEase);

            Tween logoMoveTween = _logoRectTransform
                .DOAnchorPos(_logoShownAnchoredPositionPixels, _moveDurationSeconds)
                .SetEase(_moveEase);

            Tween sliderMoveTween = _loadingSliderRectTransform
                .DOAnchorPos(_loadingSliderShownAnchoredPositionPixels, _moveDurationSeconds)
                .SetEase(_moveEase);

            Tween fakeLoadingTween = _loadingProgressSlider
                .DOValue(1f, _fakeLoadingDurationSeconds)
                .SetEase(_fakeLoadingEase);

            await UniTask.WhenAll(
                fadeTween.ToUniTask(),
                logoMoveTween.ToUniTask(),
                sliderMoveTween.ToUniTask(),
                fakeLoadingTween.ToUniTask());
        }

        private async UniTask HideAsync()
        {
            KillActiveTweens();

            await _transitionCanvasGroup
                .DOFade(0f, _fadeOutDurationSeconds)
                .SetEase(_fadeOutEase)
                .ToUniTask();

            ApplyHiddenLayout();
            _loadingProgressSlider.normalizedValue = 0f;
            _transitionCanvasGroup.gameObject.SetActive(false);
        }

        private void CacheShownLayout()
        {
            _logoShownAnchoredPositionPixels = _logoRectTransform.anchoredPosition;
            _loadingSliderShownAnchoredPositionPixels = _loadingSliderRectTransform.anchoredPosition;
        }

        private void ApplyHiddenLayout()
        {
            _logoRectTransform.anchoredPosition = _logoShownAnchoredPositionPixels + _logoHiddenOffsetPixels;
            _loadingSliderRectTransform.anchoredPosition =
                _loadingSliderShownAnchoredPositionPixels + _loadingSliderHiddenOffsetPixels;
        }

        private void KillActiveTweens()
        {
            _transitionCanvasGroup.DOKill();
            _logoRectTransform.DOKill();
            _loadingSliderRectTransform.DOKill();
            _loadingProgressSlider.DOKill();
        }

        #endregion
    }
}
