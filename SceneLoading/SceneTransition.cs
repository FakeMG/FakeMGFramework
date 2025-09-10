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
        [SerializeField] private CanvasGroup transitionScreen;
        [SerializeField] private Image backgroundColor;
        [SerializeField] private RectTransform logo;

        [Header("Show Animation Settings")]
        [SerializeField] private float showDuration;
        [SerializeField] private Vector2 showPosition = new(0, 0);
        [SerializeField] private float showScale = 1f;
        [SerializeField] private float showRotation;
        [SerializeField] private EaseType showEaseType = EaseType.Predefined;
        [SerializeField, ShowIf("showEaseType", EaseType.Predefined)] private Ease showEase = Ease.Linear;
        [SerializeField, ShowIf("showEaseType", EaseType.CustomCurve)] private AnimationCurve showEaseCurve;

        [Header("Hide Animation Settings")]
        [SerializeField] private float hideDuration;
        [SerializeField] private Vector2 hidePosition;
        [SerializeField] private float hideScale;
        [SerializeField] private float hideRotation;
        [SerializeField] private EaseType hideEaseType = EaseType.Predefined;
        [SerializeField, ShowIf("hideEaseType", EaseType.Predefined)] private Ease hideEase = Ease.Linear;
        [SerializeField, ShowIf("hideEaseType", EaseType.CustomCurve)] private AnimationCurve hideEaseCurve;

        [Header("Fade Settings")]
        [SerializeField] private float colorFadeDuration = 0.5f;
        [SerializeField] private float transitionScreenFadeDuration = 0.25f;

        [Header("Events")]
        public UnityEvent onShowAnimationStart;
        public UnityEvent onShowAnimationComplete;
        public UnityEvent onHideAnimationStart;
        public UnityEvent onHideAnimationComplete;

        private void Start()
        {
            transitionScreen.gameObject.SetActive(false);
            transitionScreen.alpha = 0f;

            logo.anchoredPosition = hidePosition;
            logo.localScale = Vector3.one * hideScale;
            logo.rotation = Quaternion.Euler(0, 0, hideRotation);
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
            onShowAnimationStart?.Invoke();

            transitionScreen.gameObject.SetActive(true);
            transitionScreen.alpha = 1f;

            backgroundColor.color =
                new Color(backgroundColor.color.r, backgroundColor.color.g, backgroundColor.color.b, 0);

            var positionUniTask =
                ApplyEase(logo.DOAnchorPos(showPosition, showDuration), showEaseType, showEase, showEaseCurve);
            var scaleUniTask =
                ApplyEase(logo.DOScale(Vector3.one * showScale, showDuration), showEaseType, showEase, showEaseCurve);
            var rotateUniTask =
                ApplyEase(logo.DORotate(new Vector3(0, 0, showRotation), showDuration, RotateMode.FastBeyond360),
                    showEaseType, showEase, showEaseCurve);

            await UniTask.WhenAll(positionUniTask, scaleUniTask, rotateUniTask);
            await backgroundColor.DOFade(1f, colorFadeDuration).ToUniTask();

            onShowAnimationComplete?.Invoke();
        }

        private async UniTask HideAsync()
        {
            onHideAnimationStart?.Invoke();

            await backgroundColor.DOFade(0f, colorFadeDuration).ToUniTask();

            var positionUniTask =
                ApplyEase(logo.DOAnchorPos(hidePosition, hideDuration), hideEaseType, hideEase, hideEaseCurve);
            var scaleUniTask =
                ApplyEase(logo.DOScale(Vector3.one * hideScale, hideDuration), hideEaseType, hideEase, hideEaseCurve);
            var rotateUniTask =
                ApplyEase(logo.DORotate(new Vector3(0, 0, hideRotation), hideDuration, RotateMode.FastBeyond360),
                    hideEaseType, hideEase, hideEaseCurve);

            await UniTask.WhenAll(positionUniTask, scaleUniTask, rotateUniTask);
            await transitionScreen.DOFade(0f, transitionScreenFadeDuration).ToUniTask();

            transitionScreen.gameObject.SetActive(false);
            onHideAnimationComplete?.Invoke();
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