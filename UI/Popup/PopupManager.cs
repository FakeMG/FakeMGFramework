using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace FakeMG.FakeMGFramework.UI.Popup
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
        private readonly Dictionary<PopupSO, PopupAnimator> _openPopups = new();
        [ShowInInspector]
        private readonly Dictionary<PopupSO, PopupAnimator> _loadedPopups = new();
        private readonly Dictionary<PopupSO, AsyncOperationHandle<GameObject>> _assetHandles = new();

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

        private void BeforeStart(PopupSO popupSO)
        {
            _openPopups[popupSO] = _loadedPopups[popupSO];
            UpdateSiblingOrderBeforeShow(popupSO);
            TryShowBackground();
            OnShowStart?.Invoke();
        }

        private void AfterShow(PopupSO popupSO)
        {
            OnShowFinished?.Invoke();
        }

        private void BeforeHide(PopupSO popupSO)
        {
            TryHideBackground();
            OnHideStart?.Invoke();
        }

        private void AfterHide(PopupSO popupSO)
        {
            UpdateSiblingOrderAfterHide(popupSO);
            _openPopups.Remove(popupSO);
            OnHideFinished?.Invoke();
        }

        public async UniTask<PopupAnimator> LoadAndInstantiatePopupAsync(PopupSO popupSO)
        {
            // Check if already loaded
            if (_loadedPopups.TryGetValue(popupSO, out var existingPopup) && existingPopup != null)
            {
                return existingPopup;
            }

            // Load the popup prefab
            var handle = Addressables.LoadAssetAsync<GameObject>(popupSO.PopupPrefabAsset);
            await handle;

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"Failed to load popup prefab: {popupSO.name}");
                return null;
            }

            // Instantiate the popup
            var popupGameObject = Instantiate(handle.Result, transform);
            var popupAnimator = popupGameObject.GetComponent<PopupAnimator>();

            if (popupAnimator == null)
            {
                Debug.LogError(
                    $"Loaded popup prefab does not have a PopupAnimator component! Popup: {popupSO.name}");
                Destroy(popupGameObject);
                Addressables.Release(handle);
                return null;
            }

            // Cache the popup and asset handle
            _loadedPopups[popupSO] = popupAnimator;
            _assetHandles[popupSO] = handle;

            // Initially hide the popup without animation
            popupAnimator.Hide(false);

            popupAnimator.OnShowStart += () => BeforeStart(popupSO);
            popupAnimator.OnShowFinished += () => AfterShow(popupSO);
            popupAnimator.OnHideStart += () => BeforeHide(popupSO);
            popupAnimator.OnHideFinished += () => AfterHide(popupSO);

            return popupAnimator;
        }

        public void UnloadPopup(PopupSO popupSO)
        {
            if (!_loadedPopups.TryGetValue(popupSO, out var popupAnimator))
            {
                Debug.LogWarning($"Popup {popupSO.name} is not loaded!");
                return;
            }

            Destroy(popupAnimator.gameObject);
            _loadedPopups.Remove(popupSO);

            _assetHandles[popupSO].Release();
            _assetHandles.Remove(popupSO);
        }

        public void ShowPopupAsync(PopupSO popupSO, bool animate = true)
        {
            if (!_loadedPopups.TryGetValue(popupSO, out var popupAnimator))
            {
                Debug.LogWarning($"Popup {popupSO.name} is not loaded!");
                return;
            }

            popupAnimator.Show(animate);
        }

        public void HidePopupAsync(PopupSO popupSO, bool animate = true)
        {
            if (!_loadedPopups.TryGetValue(popupSO, out var popupAnimator))
            {
                Debug.LogWarning($"Popup {popupSO.name} is not loaded!");
                return;
            }

            popupAnimator.Hide(animate);
        }

        private void UpdateSiblingOrderBeforeShow(PopupSO popupSO)
        {
            blackBackground.transform.SetSiblingIndex(_openPopups.Count - 1);
            _openPopups[popupSO].transform.SetSiblingIndex(_openPopups.Count);
        }

        private void UpdateSiblingOrderAfterHide(PopupSO popupSO)
        {
            int index = _openPopups[popupSO].transform.GetSiblingIndex();
            bool isLastPopup = index >= _openPopups.Count - 1;
            if (_openPopups.Count > 0 && isLastPopup)
            {
                blackBackground.transform.SetSiblingIndex(_openPopups.Count - 2);
            }

            _openPopups[popupSO].transform.SetAsLastSibling();
        }

        private void TryShowBackground()
        {
            // When showing a popup while the last popup is hiding,
            // the background is fading out and still active
            // we still want to show the background. So no check for activeInHierarchy
            // if (blackBackground.gameObject.activeInHierarchy) return;

            if (_openPopups.Count > 1) return;

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