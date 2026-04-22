using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace FakeMG.Framework.UI.Popup
{
    /// <summary>
    /// Handles background stacking, popup creation/destruction.
    /// Two black backgrounds alternate as popups stack. Order is tracked explicitly via a
    /// Layer list — sibling indices are only written, never read back for logic.
    /// </summary>
    public class PopupManager : MonoBehaviour
    {
        [Required]
        [SerializeField] private Image _blackBackgroundA;
        [Required]
        [SerializeField] private Image _blackBackgroundB;

        [Header("Debug")]
        [SerializeField] private bool _enableLogging;

        public event Action OnShowStart;
        public event Action OnShowFinished;
        public event Action OnHideStart;
        public event Action OnHideFinished;
        public event Action OnLastPopupHideStart;

        [ShowInInspector, ReadOnly]
        private readonly Dictionary<AssetReferenceT<GameObject>, PopupAnimator> _openPopups = new();
        [ShowInInspector, ReadOnly]
        private readonly Dictionary<AssetReferenceT<GameObject>, PopupAnimator> _loadedPopups = new();
        private readonly Dictionary<AssetReferenceT<GameObject>, AsyncOperationHandle<GameObject>> _assetHandles = new();
        private readonly List<PopupAnimator> _hideAllBuffer = new();

        // Each entry pairs a popup with the BG assigned to sit behind it.
        // Index 0 = bottom-most layer. This is the single source of truth for order.
        private readonly struct Layer
        {
            public readonly PopupAnimator Popup;
            public readonly Image Background;

            public Layer(PopupAnimator popup, Image background)
            {
                Popup = popup;
                Background = background;
            }
        }

        private readonly List<Layer> _layers = new();

        private const float BACKGROUND_FADE_DURATION = 0.3f;
        private float _backgroundFadeAlpha;

        // -----------------------------------------------------------------------------------------
        // Unity lifecycle
        // -----------------------------------------------------------------------------------------

        private void Start()
        {
            _backgroundFadeAlpha = _blackBackgroundA.color.a;

            SetAlpha(_blackBackgroundA, 0f);
            SetAlpha(_blackBackgroundB, 0f);
            _blackBackgroundA.gameObject.SetActive(false);
            _blackBackgroundB.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            foreach (var handle in _assetHandles.Values)
            {
                if (handle.IsValid())
                    Addressables.Release(handle);
            }

            _assetHandles.Clear();
            _openPopups.Clear();
            _loadedPopups.Clear();
            _layers.Clear();
        }

        // -----------------------------------------------------------------------------------------
        // Show / Hide callbacks
        // -----------------------------------------------------------------------------------------

        private void BeforeShow(AssetReferenceT<GameObject> popupPrefabAsset)
        {
            _openPopups[popupPrefabAsset] = _loadedPopups[popupPrefabAsset];
            PushBackground(_openPopups[popupPrefabAsset]);
            OnShowStart?.Invoke();
        }

        private void AfterShow(AssetReferenceT<GameObject> popupPrefabAsset)
        {
            OnShowFinished?.Invoke();
        }

        private void BeforeHide(AssetReferenceT<GameObject> popupPrefabAsset)
        {
            if (_openPopups.Count == 1)
                OnLastPopupHideStart?.Invoke();

            PopBackground();
            OnHideStart?.Invoke();
        }

        private void AfterHide(AssetReferenceT<GameObject> popupPrefabAsset)
        {
            if (!_openPopups.ContainsKey(popupPrefabAsset))
            {
                OnHideFinished?.Invoke();
                return;
            }

            _openPopups.Remove(popupPrefabAsset);
            OnHideFinished?.Invoke();
        }

        // -----------------------------------------------------------------------------------------
        // Background stack
        // -----------------------------------------------------------------------------------------

        private void PushBackground(PopupAnimator popup)
        {
            Image incoming = NextBackground();

            // Fade out the current top BG. Do NOT reposition it — moving it mid-fade
            // cuts the fade animation visually.
            if (_layers.Count > 0)
                FadeOut(_layers[^1].Background, disable: true);

            _layers.Add(new Layer(popup, incoming));

            // BG last, then popup last — order of two SetAsLastSibling calls is unambiguous:
            // result is always [..., BG, Popup] regardless of where they started.
            incoming.transform.SetAsLastSibling();
            popup.transform.SetAsLastSibling();

            incoming.DOKill();
            incoming.gameObject.SetActive(true);
            incoming.DOFade(_backgroundFadeAlpha, BACKGROUND_FADE_DURATION)
                .SetLink(incoming.gameObject);
        }

        private void PopBackground()
        {
            if (_layers.Count == 0)
                return;

            Image outgoing = _layers[^1].Background;
            _layers.RemoveAt(_layers.Count - 1);

            // Fade the outgoing BG out in place — do NOT reposition it.
            FadeOut(outgoing, disable: true);

            if (_layers.Count > 0)
            {
                Layer topLayer = _layers[^1];

                int popupIndex = topLayer.Popup.transform.GetSiblingIndex();
                int bgIndex = topLayer.Background.transform.GetSiblingIndex();

                // FIX: Safely slot the BG directly in front of its popup accounting for Unity's sibling shift.
                // If BG is currently lower than the popup, moving it to popupIndex shifts the popup down (-1),
                // placing the BG in front. We subtract 1 to keep it strictly behind the popup.
                int targetIndex = bgIndex < popupIndex ? popupIndex - 1 : popupIndex;
                topLayer.Background.transform.SetSiblingIndex(targetIndex);

                topLayer.Background.DOKill();
                topLayer.Background.gameObject.SetActive(true);
                topLayer.Background.DOFade(_backgroundFadeAlpha, BACKGROUND_FADE_DURATION)
                    .SetLink(topLayer.Background.gameObject);
            }
        }

        /// <summary>
        /// Returns whichever of A/B is not assigned to the current top layer.
        /// </summary>
        private Image NextBackground()
        {
            if (_layers.Count == 0)
                return _blackBackgroundA;

            return _layers[^1].Background == _blackBackgroundA
                ? _blackBackgroundB
                : _blackBackgroundA;
        }

        private void FadeOut(Image bg, bool disable)
        {
            bg.DOKill();
            var tween = bg.DOFade(0f, BACKGROUND_FADE_DURATION).SetLink(bg.gameObject);

            if (disable)
                tween.OnComplete(() =>
                {
                    if (bg != null)
                        bg.gameObject.SetActive(false);
                });
        }

        private static void SetAlpha(Image image, float alpha)
        {
            Color c = image.color;
            c.a = alpha;
            image.color = c;
        }

        // -----------------------------------------------------------------------------------------
        // Public API
        // -----------------------------------------------------------------------------------------

        public async UniTask<GameObject> LoadAndInstantiatePopupAsync(AssetReferenceT<GameObject> popupPrefabAsset)
        {
            if (_loadedPopups.TryGetValue(popupPrefabAsset, out var existingPopup) && existingPopup != null)
                return existingPopup.gameObject;

            var handle = Addressables.LoadAssetAsync<GameObject>(popupPrefabAsset);
            await handle;

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Echo.Error($"Failed to load popup prefab: {popupPrefabAsset}", _enableLogging);
                return null;
            }

            GameObject popupGameObject = Instantiate(handle.Result, transform);

            if (!popupGameObject.TryGetComponent(out PopupAnimator popupAnimator))
            {
                Echo.Error(
                    $"Loaded popup prefab does not have a PopupAnimator component! Popup: {popupGameObject.name}",
                    _enableLogging);
                Destroy(popupGameObject);
                Addressables.Release(handle);
                return null;
            }

            _loadedPopups[popupPrefabAsset] = popupAnimator;
            _assetHandles[popupPrefabAsset] = handle;

            await popupAnimator.Hide(false);

            popupAnimator.OnShowStart += () => BeforeShow(popupPrefabAsset);
            popupAnimator.OnShowFinished += () => AfterShow(popupPrefabAsset);
            popupAnimator.OnHideStart += () => BeforeHide(popupPrefabAsset);
            popupAnimator.OnHideFinished += () => AfterHide(popupPrefabAsset);

            return popupGameObject;
        }

        public void UnloadPopup(AssetReferenceT<GameObject> popupPrefabAsset)
        {
            if (!_loadedPopups.TryGetValue(popupPrefabAsset, out var popupAnimator))
            {
                Echo.Warning($"Popup {popupPrefabAsset} is not loaded!", _enableLogging);
                return;
            }

            if (_openPopups.ContainsKey(popupPrefabAsset))
            {
                PopBackground();
                _openPopups.Remove(popupPrefabAsset);
            }

            Destroy(popupAnimator.gameObject);
            _loadedPopups.Remove(popupPrefabAsset);

            if (_assetHandles.TryGetValue(popupPrefabAsset, out AsyncOperationHandle<GameObject> handle))
            {
                if (handle.IsValid())
                    Addressables.Release(handle);

                _assetHandles.Remove(popupPrefabAsset);
            }
        }

        public async UniTask ShowPopupAsync(AssetReferenceT<GameObject> popupPrefabAsset, bool animate = true)
        {
            if (!_loadedPopups.TryGetValue(popupPrefabAsset, out PopupAnimator popupAnimator))
            {
                Echo.Warning($"Popup {popupPrefabAsset} is not loaded!", _enableLogging);
                return;
            }

            await popupAnimator.Show(animate);
        }

        public async UniTask HidePopupAsync(AssetReferenceT<GameObject> popupPrefabAsset, bool animate = true)
        {
            if (!_loadedPopups.TryGetValue(popupPrefabAsset, out PopupAnimator popupAnimator))
            {
                Echo.Warning($"Popup {popupPrefabAsset} is not loaded!", _enableLogging);
                return;
            }

            await popupAnimator.Hide(animate);
        }

        public async UniTask HideAllPopupsAsync(bool animate = true)
        {
            if (_openPopups.Count == 0)
                return;

            _hideAllBuffer.Clear();
            foreach (PopupAnimator popupAnimator in _openPopups.Values)
            {
                if (popupAnimator)
                    _hideAllBuffer.Add(popupAnimator);
            }

            // Sort by layer index descending — top layer first.
            _hideAllBuffer.Sort((a, b) =>
            {
                int ia = _layers.FindIndex(l => l.Popup == a);
                int ib = _layers.FindIndex(l => l.Popup == b);
                return ib.CompareTo(ia);
            });

            foreach (PopupAnimator popupAnimator in _hideAllBuffer)
                await popupAnimator.Hide(animate);

            _hideAllBuffer.Clear();
        }
    }
}
