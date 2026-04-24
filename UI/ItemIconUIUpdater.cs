using System;
using Cysharp.Threading.Tasks;
using FakeMG.Framework.ExtensionMethods;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace FakeMG.Framework.UI
{
    public class ItemIconUIUpdater : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _countText;
        [SerializeField] private bool _showCountWhenZero;

        private AsyncOperationHandle<Sprite>? _loadedSpriteHandle;
        private int _updateRequestVersion;

        #region Unity Lifecycle

        private void OnDestroy()
        {
            UnloadHandle();
        }

        #endregion

        #region Public Methods

        public void UpdateUI(Sprite newIcon, int count)
        {
            InvalidatePendingRequests();
            UnloadHandle();
            _icon.sprite = newIcon;
            ApplyCountPresentation(
                count > 0 ? count.ToShorthand() : string.Empty,
                _showCountWhenZero || count > 0);
        }

        public void UpdateUI(Sprite newIcon, string count)
        {
            InvalidatePendingRequests();
            UnloadHandle();
            _icon.sprite = newIcon;
            ApplyCountPresentation(
                count,
                _showCountWhenZero || !string.IsNullOrEmpty(count));
        }

        public async UniTask UpdateUIAsync(ItemSO item, int count)
        {
            await UpdateUIAsync(
                item,
                count.ToShorthand(),
                _showCountWhenZero || count > 0);
        }

        public async UniTask UpdateUIAsync(ItemSO item, string countText, bool isCountVisible)
        {
            int requestVersion = InvalidatePendingRequests();
            UnloadHandle();

            if (item.IconSpriteAsset != null && item.IconSpriteAsset.RuntimeKeyIsValid())
            {
                AsyncOperationHandle<Sprite> spriteHandle = Addressables.LoadAssetAsync<Sprite>(item.IconSpriteAsset);

                try
                {
                    Sprite sprite = await spriteHandle.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());

                    if (spriteHandle.Status != AsyncOperationStatus.Succeeded)
                    {
                        Echo.Error($"Failed to load sprite for item '{item.name}'.");
                        Addressables.Release(spriteHandle);
                        return;
                    }

                    if (!_icon || !_countText)
                    {
                        Addressables.Release(spriteHandle);
                        return;
                    }

                    if (requestVersion != _updateRequestVersion)
                    {
                        Addressables.Release(spriteHandle);
                        return;
                    }

                    _icon.sprite = sprite;
                    _loadedSpriteHandle = spriteHandle;
                }
                catch (OperationCanceledException)
                {
                    if (spriteHandle.IsValid())
                    {
                        Addressables.Release(spriteHandle);
                    }

                    return;
                }
            }
            else
            {
                Echo.Error($"Invalid icon sprite reference for item '{item.name}'.");
            }

            if (requestVersion != _updateRequestVersion)
            {
                return;
            }

            ApplyCountPresentation(countText, isCountVisible);
        }

        #endregion

        #region Private Methods

        private void ApplyCountPresentation(string countText, bool isCountVisible)
        {
            _countText.text = countText;
            _countText.gameObject.SetActive(isCountVisible);
        }

        private int InvalidatePendingRequests()
        {
            _updateRequestVersion++;
            return _updateRequestVersion;
        }

        private void UnloadHandle()
        {
            if (_loadedSpriteHandle.HasValue && _loadedSpriteHandle.Value.IsValid())
            {
                Addressables.Release(_loadedSpriteHandle.Value);
                _loadedSpriteHandle = null;
            }
        }

        #endregion
    }
}
