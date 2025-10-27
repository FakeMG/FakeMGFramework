using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FakeMG.Framework.SceneLoading
{
    public enum EaseType
    {
        Predefined,
        CustomCurve
    }

    public class SceneTransition : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _transitionScreen;
        [SerializeField] private Image _backgroundColor;
        [SerializeField] private RectTransform _logo;

        [Header("Show Animation Settings")]
        [SerializeField] private float _showDuration;
        [SerializeField] private Vector2 _showPosition = new(0, 0);
        [SerializeField] private float _showScale = 1f;
        [SerializeField] private float _showRotation;
        [SerializeField] private EaseType _showEaseType = EaseType.Predefined;
        [SerializeField, ShowIf("showEaseType", EaseType.Predefined)] private Ease _showEase = Ease.Linear;
        [SerializeField, ShowIf("showEaseType", EaseType.CustomCurve)] private AnimationCurve _showEaseCurve;

        [Header("Hide Animation Settings")]
        [SerializeField] private float _hideDuration;
        [SerializeField] private Vector2 _hidePosition;
        [SerializeField] private float _hideScale;
        [SerializeField] private float _hideRotation;
        [SerializeField] private EaseType _hideEaseType = EaseType.Predefined;
        [SerializeField, ShowIf("hideEaseType", EaseType.Predefined)] private Ease _hideEase = Ease.Linear;
        [SerializeField, ShowIf("hideEaseType", EaseType.CustomCurve)] private AnimationCurve _hideEaseCurve;

        [Header("Fade Settings")]
        [SerializeField] private float _colorFadeDuration = 0.5f;
        [SerializeField] private float _transitionScreenFadeDuration = 0.25f;

        [Header("Events")]
        public UnityEvent OnShowAnimationStart;
        public UnityEvent OnShowAnimationComplete;
        public UnityEvent OnHideAnimationStart;
        public UnityEvent OnHideAnimationComplete;

        private void Start()
        {
            _transitionScreen.gameObject.SetActive(false);
            _transitionScreen.alpha = 0f;

            _logo.anchoredPosition = _hidePosition;
            _logo.localScale = Vector3.one * _hideScale;
            _logo.rotation = Quaternion.Euler(0, 0, _hideRotation);
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

        public async UniTask PlayTransitionAsync(Action process)
        {
            await ShowAsync();
            process?.Invoke();
            await HideAsync();
        }

        public async UniTask PlayTransitionAsync(Func<UniTask> processTask)
        {
            await ShowAsync();

            if (processTask != null)
            {
                await processTask();
            }

            await HideAsync();
        }

        //TODO: protection from multiple calls or call Hide() before Show() finishs
        private async UniTask ShowAsync()
        {
            OnShowAnimationStart?.Invoke();

            _transitionScreen.gameObject.SetActive(true);
            _transitionScreen.alpha = 1f;

            _backgroundColor.color =
                new Color(_backgroundColor.color.r, _backgroundColor.color.g, _backgroundColor.color.b, 0);

            var positionUniTask =
                ApplyEase(_logo.DOAnchorPos(_showPosition, _showDuration), _showEaseType, _showEase, _showEaseCurve);
            var scaleUniTask =
                ApplyEase(_logo.DOScale(Vector3.one * _showScale, _showDuration), _showEaseType, _showEase, _showEaseCurve);
            var rotateUniTask =
                ApplyEase(_logo.DORotate(new Vector3(0, 0, _showRotation), _showDuration, RotateMode.FastBeyond360),
                    _showEaseType, _showEase, _showEaseCurve);

            await UniTask.WhenAll(positionUniTask, scaleUniTask, rotateUniTask);
            await _backgroundColor.DOFade(1f, _colorFadeDuration).ToUniTask();

            OnShowAnimationComplete?.Invoke();
        }

        private async UniTask HideAsync()
        {
            OnHideAnimationStart?.Invoke();

            await _backgroundColor.DOFade(0f, _colorFadeDuration).ToUniTask();

            var positionUniTask =
                ApplyEase(_logo.DOAnchorPos(_hidePosition, _hideDuration), _hideEaseType, _hideEase, _hideEaseCurve);
            var scaleUniTask =
                ApplyEase(_logo.DOScale(Vector3.one * _hideScale, _hideDuration), _hideEaseType, _hideEase, _hideEaseCurve);
            var rotateUniTask =
                ApplyEase(_logo.DORotate(new Vector3(0, 0, _hideRotation), _hideDuration, RotateMode.FastBeyond360),
                    _hideEaseType, _hideEase, _hideEaseCurve);

            await UniTask.WhenAll(positionUniTask, scaleUniTask, rotateUniTask);
            await _transitionScreen.DOFade(0f, _transitionScreenFadeDuration).ToUniTask();

            _transitionScreen.gameObject.SetActive(false);
            OnHideAnimationComplete?.Invoke();
        }

        private UniTask ApplyEase(Tween tween, EaseType easeType, Ease predefinedEase, AnimationCurve customCurve)
        {
            if (easeType == EaseType.Predefined)
            {
                tween.SetEase(predefinedEase);
            }
            else
            {
                tween.SetEase(customCurve);
            }

            return tween.ToUniTask();
        }
    }
}