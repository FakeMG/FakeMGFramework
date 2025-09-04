using System;
using System.Collections;
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
        [SerializeField] private float showRotation = 0f;
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

        public void Show(Action process)
        {
            StartCoroutine(ShowWithProcess(process));
        }

        public void Show(Func<IEnumerator> processCoroutine)
        {
            StartCoroutine(ShowWithProcessCoroutine(processCoroutine));
        }

        [Button]
        public void Show()
        {
            StartCoroutine(ShowAnimationCoroutine());
        }

        [Button]
        public void Hide()
        {
            StartHideAnimation();
        }

        private IEnumerator ShowWithProcess(Action process)
        {
            yield return StartCoroutine(ShowAnimationCoroutine());
            process?.Invoke();
            StartHideAnimation();
        }

        private IEnumerator ShowWithProcessCoroutine(Func<IEnumerator> processCoroutine)
        {
            yield return StartCoroutine(ShowAnimationCoroutine());
            if (processCoroutine != null)
            {
                yield return StartCoroutine(processCoroutine());
            }

            StartHideAnimation();
        }

        private IEnumerator ShowAnimationCoroutine()
        {
            onShowAnimationStart?.Invoke();

            transitionScreen.gameObject.SetActive(true);
            transitionScreen.alpha = 1f;

            backgroundColor.color =
                new Color(backgroundColor.color.r, backgroundColor.color.g, backgroundColor.color.b, 0);

            var positionTween = ApplyEase(logo.DOAnchorPos(showPosition, showDuration), showEaseType, showEase,
                showEaseCurve);
            ApplyEase(logo.DOScale(Vector3.one * showScale, showDuration), showEaseType, showEase, showEaseCurve);
            ApplyEase(logo.DORotate(new Vector3(0, 0, showRotation), showDuration, RotateMode.FastBeyond360),
                showEaseType, showEase, showEaseCurve);

            yield return positionTween.WaitForCompletion();
            yield return backgroundColor.DOFade(1f, colorFadeDuration).WaitForCompletion();

            onShowAnimationComplete?.Invoke();
        }

        private void StartHideAnimation()
        {
            onHideAnimationStart?.Invoke();

            backgroundColor.DOFade(0f, colorFadeDuration).OnComplete(() =>
            {
                ApplyEase(logo.DOAnchorPos(hidePosition, hideDuration), hideEaseType, hideEase, hideEaseCurve);
                ApplyEase(logo.DOScale(Vector3.one * hideScale, hideDuration), hideEaseType, hideEase, hideEaseCurve);
                ApplyEase(logo.DORotate(new Vector3(0, 0, hideRotation), hideDuration, RotateMode.FastBeyond360),
                    hideEaseType, hideEase, hideEaseCurve);

                transitionScreen.DOFade(0f, transitionScreenFadeDuration).SetDelay(hideDuration).OnComplete(() =>
                {
                    transitionScreen.gameObject.SetActive(false);
                    onHideAnimationComplete?.Invoke();
                });
            });
        }

        private Tween ApplyEase(Tween tween, EaseType easeType, Ease predefinedEase, AnimationCurve customCurve)
        {
            if (easeType == EaseType.Predefined)
            {
                tween.SetEase(predefinedEase);
            }
            else
            {
                tween.SetEase(customCurve);
            }

            return tween;
        }
    }
}