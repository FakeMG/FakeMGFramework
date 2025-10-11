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
        private const float BACKGROUND_FADE_DURATION = 0.3f;
        private float _backgroundFadeAlpha = 0.95f;

        [Required]
        [SerializeField] private Image blackBackground;

        public event Action OnShowStart;
        public event Action OnShowFinished;
        public event Action OnHideStart;
        public event Action OnHideFinished;

        [ShowInInspector]
        private readonly Dictionary<AssetReferenceT<GameObject>, PopupAnimator> _openPopups = new();
        [ShowInInspector]
        private readonly Dictionary<AssetReferenceT<GameObject>, PopupAnimator> _loadedPopups = new();
        private readonly Dictionary<AssetReferenceT<GameObject>, AsyncOperationHandle<GameObject>> _assetHandles = new();

        private void Start()
        {
            _backgroundFadeAlpha = blackBackground.color.a;

            Color backgroundColor = blackBackground.color;
            backgroundColor.a = 0f;
            blackBackground.color = backgroundColor;
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

        private void BeforeStart(AssetReferenceT<GameObject> popupPrefabAsset)
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
                Debug.LogError($"Failed to load popup prefab: {popupPrefabAsset}");
                return null;
            }

            // Instantiate the popup
            var popupGameObject = Instantiate(handle.Result, transform);

            if (!popupGameObject.TryGetComponent<PopupAnimator>(out var popupAnimator))
            {
                Debug.LogError(
                    $"Loaded popup prefab does not have a PopupAnimator component! Popup: {popupGameObject.name}");
                Destroy(popupGameObject);
                Addressables.Release(handle);
                return null;
            }

            // Cache the popup and asset handle
            _loadedPopups[popupPrefabAsset] = popupAnimator;
            _assetHandles[popupPrefabAsset] = handle;

            // Initially hide the popup without animation
            await popupAnimator.Hide(false);

            popupAnimator.OnShowStart += () => BeforeStart(popupPrefabAsset);
            popupAnimator.OnShowFinished += () => AfterShow(popupPrefabAsset);
            popupAnimator.OnHideStart += () => BeforeHide(popupPrefabAsset);
            popupAnimator.OnHideFinished += () => AfterHide(popupPrefabAsset);

            return popupGameObject;
        }

        public void UnloadPopup(AssetReferenceT<GameObject> popupPrefabAsset)
        {
            if (!_loadedPopups.TryGetValue(popupPrefabAsset, out var popupAnimator))
            {
                Debug.LogWarning($"Popup {popupPrefabAsset} is not loaded!");
                return;
            }

            Destroy(popupAnimator.gameObject);
            _loadedPopups.Remove(popupPrefabAsset);

            _assetHandles[popupPrefabAsset].Release();
            _assetHandles.Remove(popupPrefabAsset);
        }

        public async UniTask ShowPopupAsync(AssetReferenceT<GameObject> popupPrefabAsset, bool animate = true)
        {
            if (!_loadedPopups.TryGetValue(popupPrefabAsset, out var popupAnimator))
            {
                Debug.LogWarning($"Popup {popupPrefabAsset} is not loaded!");
                return;
            }

            await popupAnimator.Show(animate);
        }

        public async UniTask HidePopupAsync(AssetReferenceT<GameObject> popupPrefabAsset, bool animate = true)
        {
            if (!_loadedPopups.TryGetValue(popupPrefabAsset, out var popupAnimator))
            {
                Debug.LogWarning($"Popup {popupPrefabAsset} is not loaded!");
                return;
            }

            await popupAnimator.Hide(animate);
        }

        private void UpdateSiblingOrderBeforeShow(AssetReferenceT<GameObject> popupPrefabAsset)
        {
            blackBackground.transform.SetSiblingIndex(_openPopups.Count - 1);
            _openPopups[popupPrefabAsset].transform.SetSiblingIndex(_openPopups.Count);
        }

        private void UpdateSiblingOrderAfterHide(AssetReferenceT<GameObject> popupPrefabAsset)
        {
            int index = _openPopups[popupPrefabAsset].transform.GetSiblingIndex();
            bool isLastPopup = index >= _openPopups.Count - 1;
            if (_openPopups.Count > 0 && isLastPopup)
            {
                blackBackground.transform.SetSiblingIndex(_openPopups.Count - 2);
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

            blackBackground.DOKill();
            blackBackground.gameObject.SetActive(true);

            blackBackground.DOFade(_backgroundFadeAlpha, BACKGROUND_FADE_DURATION)
                .SetLink(blackBackground.gameObject);
        }

        private void TryHideBackground()
        {
            if (!blackBackground.gameObject.activeInHierarchy) return;
            if (_openPopups.Count > 1) return;

            blackBackground.DOKill();

            blackBackground.DOFade(0f, BACKGROUND_FADE_DURATION).SetLink(blackBackground.gameObject).OnComplete(() =>
            {
                blackBackground.gameObject.SetActive(false);
            });
        }
    }
}