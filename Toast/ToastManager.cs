using FakeMG.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Pool;

namespace FakeMG.Toast
{
    /// <summary>
    /// Globally accessible toast service. Shows short, non-blocking, auto-hiding text messages
    /// stacked at the configured container anchor. Registered in ManagerLifetimeScope.
    /// </summary>
    public class ToastManager : MonoBehaviour
    {
        [Required]
        [SerializeField] private ToastConfigSO _configSO;
        [Required]
        [SerializeField] private ToastView _toastViewPrefab;
        [Required]
        [SerializeField] private RectTransform _containerRectTransform;

        private ObjectPool<ToastView> _pool;
        // Newest first: index 0 is the toast at the entry position.
        private readonly List<ActiveToast> _activeToasts = new();
        private CancellationTokenSource _lifetimeCts;
        private bool _isReady;

        private class ActiveToast
        {
            public ToastView View;
            public Vector2 VisibleSizePixels;
            public CancellationTokenSource TimerCts;
        }

        #region Lifecycle
        private void Awake()
        {
            _lifetimeCts = new CancellationTokenSource();
            _pool = new ObjectPool<ToastView>(
                CreatePooledView,
                ActivatePooledView,
                ResetPooledView,
                DestroyPooledView,
                collectionCheck: true,
                defaultCapacity: _configSO.MaxVisibleCount,
                maxSize: _configSO.PoolMaxSize);
            _isReady = true;
        }

        private void OnDestroy()
        {
            _lifetimeCts.Cancel();
            _lifetimeCts.Dispose();
            _pool.Dispose();
        }
        #endregion

        #region Public
        public void Show(string text)
        {
            Show(text, ToastType.Info);
        }

        public void Show(string text, ToastType type)
        {
            if (IsNotReady()) return;
            ShowInternalAsync(text, _configSO.GetColorFor(type)).Forget();
        }

        public void Show(string text, Color customTextColor)
        {
            if (IsNotReady()) return;
            ShowInternalAsync(text, customTextColor).Forget();
        }

        /// <summary>Clears every active toast through the normal hide animation flow.</summary>
        public void ClearAll()
        {
            if (IsNotReady()) return;

            ActiveToast[] snapshot = _activeToasts.ToArray();
            foreach (ActiveToast toast in snapshot)
            {
                HideToastAsync(toast).Forget();
            }
        }
        #endregion

        #region Private
        private bool IsNotReady()
        {
            if (_isReady) return false;

            Echo.Warning(
                "Toast requested before the toast container is ready. This is a setup error; the request is ignored.",
                context: this);
            return true;
        }

        private async UniTask ShowInternalAsync(string text, Color textColor)
        {
            bool hasViewAvailable = _pool.CountInactive > 0 || _pool.CountActive < _configSO.PoolMaxSize;
            if (!hasViewAvailable)
            {
                if (_activeToasts.Count == 0)
                {
                    Echo.Warning("Pool is at max size and every view is already hiding. Request dropped.", context: this);
                    return;
                }

                Echo.Warning("Pool is at max size. Evicting the oldest toast to make room.", context: this);
                await HideToastAsync(GetOldestToast());
                if (_lifetimeCts.IsCancellationRequested) return;
            }

            // Hiding toasts no longer count toward the limit; the new toast shows without waiting.
            while (_activeToasts.Count >= _configSO.MaxVisibleCount)
            {
                HideToastAsync(GetOldestToast()).Forget();
            }

            ToastView view = _pool.Get();
            float visibleHeightPixels = view.ApplyMessage(
                text, textColor, _configSO.MaxToastHeightPixels, _configSO.MaxLineCount);

            var toast = new ActiveToast
            {
                View = view,
                VisibleSizePixels = new Vector2(view.RectTransform.sizeDelta.x, visibleHeightPixels),
                TimerCts = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCts.Token)
            };
            _activeToasts.Insert(0, toast);

            RelayoutActiveToasts(isNewestSnappedToEntry: true);
            view.Animator.ShowAsync(toast.TimerCts.Token).Forget();
            RunDisplayTimerAsync(toast).Forget();
        }

        private async UniTask RunDisplayTimerAsync(ActiveToast toast)
        {
            bool wasCancelled = await UniTask
                .Delay(TimeSpan.FromSeconds(_configSO.DisplayDurationSeconds),
                    DelayType.UnscaledDeltaTime,
                    cancellationToken: toast.TimerCts.Token)
                .SuppressCancellationThrow();

            // On cancellation (early clear, scene change, teardown) the canceller drives the hide.
            if (wasCancelled) return;

            await HideToastAsync(toast);
        }

        // The single hide path, shared by duration expiry, eviction, early clear and scene change.
        private async UniTask HideToastAsync(ActiveToast toast)
        {
            toast.TimerCts.Cancel();
            _activeToasts.Remove(toast);
            RelayoutActiveToasts(isNewestSnappedToEntry: false);

            // Hide runs on the manager lifetime token, not the toast timer token,
            // so cancelling a toast never kills its own hide animation.
            await toast.View.Animator.HideAsync(_lifetimeCts.Token);
            toast.TimerCts.Dispose();

            if (_lifetimeCts.IsCancellationRequested) return;
            _pool.Release(toast.View);
        }

        private void RelayoutActiveToasts(bool isNewestSnappedToEntry)
        {
            var sizesPixels = new List<Vector2>(_activeToasts.Count);
            foreach (ActiveToast toast in _activeToasts)
            {
                sizesPixels.Add(toast.VisibleSizePixels);
            }

            Vector2[] offsets = ToastStackLayout.ComputeOffsets(
                sizesPixels, _configSO.StackDirection, _configSO.SpacingPixels);

            for (int i = 0; i < _activeToasts.Count; i++)
            {
                ActiveToast toast = _activeToasts[i];
                bool shouldSnap = i == 0 && isNewestSnappedToEntry;
                if (shouldSnap)
                {
                    toast.View.RectTransform.anchoredPosition = offsets[i];
                }
                else
                {
                    toast.View.Animator.MoveToAsync(offsets[i], _lifetimeCts.Token).Forget();
                }
            }
        }

        private ActiveToast GetOldestToast()
        {
            return _activeToasts[^1];
        }

        private ToastView CreatePooledView()
        {
            ToastView view = Instantiate(_toastViewPrefab, _containerRectTransform);
            // The prefab pivot only suits one direction; stacking math needs the pivot facing the configured one.
            view.RectTransform.pivot = ToastStackLayout.GetPivotForDirection(_configSO.StackDirection);
            view.gameObject.SetActive(false);
            return view;
        }

        private void ActivatePooledView(ToastView view)
        {
            view.gameObject.SetActive(true);
        }

        private void ResetPooledView(ToastView view)
        {
            view.ResetForPool();
        }

        private void DestroyPooledView(ToastView view)
        {
            Destroy(view.gameObject);
        }
        #endregion
    }
}
