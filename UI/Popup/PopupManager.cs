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
    /// </summary>
    public class PopupManager : MonoBehaviour
    {
        [SerializeField] private PopupManagerRefSO _popupManagerRefSO;
        [Required]
        [SerializeField] private Image _blackBackground;

        [Header("Debug")]
        [SerializeField] private bool _enableLogging;

        public event Action OnShowStart;
        public event Action OnShowFinished;
        public event Action OnHideStart;
        public event Action OnHideFinished;

        [ShowInInspector, ReadOnly]
        private readonly Dictionary<AssetReferenceT<GameObject>, PopupAnimator> _openPopups = new();
        [ShowInInspector, ReadOnly]
        private readonly Dictionary<AssetReferenceT<GameObject>, PopupAnimator> _loadedPopups = new();
        private readonly Dictionary<AssetReferenceT<GameObject>, AsyncOperationHandle<GameObject>> _assetHandles = new();

        private const float BACKGROUND_FADE_DURATION = 0.3f;
        private float _backgroundFadeAlpha = 0.95f;

        private void Start()
        {
            _backgroundFadeAlpha = _blackBackground.color.a;

            Color backgroundColor = _blackBackground.color;
            backgroundColor.a = 0f;
            _blackBackground.color = backgroundColor;

            _popupManagerRefSO.Set(this);
        }

        private void OnDestroy()
        {
            // Release all cached asset handles
            foreach (var handle in _assetHandles.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }

            _assetHandles.Clear();
            _loadedPopups.Clear();
        }

        private void BeforeShow(AssetReferenceT<GameObject> popupPrefabAsset)
        {
            _openPopups[popupPrefabAsset] = _loadedPopups[popupPrefabAsset];
            UpdateSiblingOrderBeforeShow(popupPrefabAsset);
            TryShowBackground();
            OnShowStart?.Invoke();
        }

        private void AfterShow(AssetReferenceT<GameObject> popupPrefabAsset)
        {
            OnShowFinished?.Invoke();
        }

        private void BeforeHide(AssetReferenceT<GameObject> popupPrefabAsset)
        {
            TryHideBackground();
            OnHideStart?.Invoke();
        }

        private void AfterHide(AssetReferenceT<GameObject> popupPrefabAsset)
        {
            UpdateSiblingOrderAfterHide(popupPrefabAsset);
            _openPopups.Remove(popupPrefabAsset);
            OnHideFinished?.Invoke();
        }

        public async UniTask<GameObject> LoadAndInstantiatePopupAsync(AssetReferenceT<GameObject> popupPrefabAsset)
        {
            // Check if already loaded
            if (_loadedPopups.TryGetValue(popupPrefabAsset, out var existingPopup) && existingPopup != null)
            {
                return existingPopup.gameObject;
            }

            // Load the popup prefab
            var handle = Addressables.LoadAssetAsync<GameObject>(popupPrefabAsset);
            await handle;

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Echo.Error($"Failed to load popup prefab: {popupPrefabAsset}", _enableLogging);
                return null;
            }

            // Instantiate the popup
            GameObject popupGameObject = Instantiate(handle.Result, transform);

            if (!popupGameObject.TryGetComponent(out PopupAnimator popupAnimator))
            {
                Echo.Error(
                    $"Loaded popup prefab does not have a PopupAnimator component! Popup: {popupGameObject.name}", _enableLogging);
                Destroy(popupGameObject);
                Addressables.Release(handle);
                return null;
            }

            // Cache the popup and asset handle
            _loadedPopups[popupPrefabAsset] = popupAnimator;
            _assetHandles[popupPrefabAsset] = handle;

            // Initially hide the popup without animation
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

            Destroy(popupAnimator.gameObject);
            _loadedPopups.Remove(popupPrefabAsset);

            _assetHandles[popupPrefabAsset].Release();
            _assetHandles.Remove(popupPrefabAsset);
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

        private void UpdateSiblingOrderBeforeShow(AssetReferenceT<GameObject> popupPrefabAsset)
        {
            _blackBackground.transform.SetSiblingIndex(_openPopups.Count - 1);
            _openPopups[popupPrefabAsset].transform.SetSiblingIndex(_openPopups.Count);
        }

        private void UpdateSiblingOrderAfterHide(AssetReferenceT<GameObject> popupPrefabAsset)
        {
            int index = _openPopups[popupPrefabAsset].transform.GetSiblingIndex();
            bool isLastPopup = index >= _openPopups.Count - 1;
            if (_openPopups.Count > 0 && isLastPopup)
            {
                _blackBackground.transform.SetSiblingIndex(_openPopups.Count - 2);
            }

            _openPopups[popupPrefabAsset].transform.SetAsLastSibling();
        }

        private void TryShowBackground()
        {
            // When showing a popup while the last popup is hiding,
            // the background is fading out and still active
            // we still want to show the background. So no check for activeInHierarchy
            // if (blackBackground.gameObject.activeInHierarchy) return;
            // Also, there are 2 popups is in the stack, we need to check _openPopups.Count > 2

            if (_openPopups.Count > 2) return;

            _blackBackground.DOKill();
            _blackBackground.gameObject.SetActive(true);

            _blackBackground.DOFade(_backgroundFadeAlpha, BACKGROUND_FADE_DURATION)
                .SetLink(_blackBackground.gameObject);
        }

        private void TryHideBackground()
        {
            if (!_blackBackground.gameObject.activeInHierarchy) return;
            if (_openPopups.Count > 1) return;

            _blackBackground.DOKill();

            _blackBackground.DOFade(0f, BACKGROUND_FADE_DURATION).SetLink(_blackBackground.gameObject).OnComplete(() =>
            {
                _blackBackground.gameObject.SetActive(false);
            });
        }
    }
}