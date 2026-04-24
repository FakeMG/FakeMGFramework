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

        #region Unity Lifecycle

        private void OnDestroy()
        {
            UnloadHandle();
        }

        #endregion

        #region Public Methods

        public void UpdateUI(Sprite newIcon, int count)
        {
            _icon.sprite = newIcon;
            _countText.text = count > 0 ? count.ToShorthand() : string.Empty;
            _countText.gameObject.SetActive(_showCountWhenZero || count > 0);
        }

        public void UpdateUI(Sprite newIcon, string count)
        {
            _icon.sprite = newIcon;
            _countText.text = count;
            _countText.gameObject.SetActive(_showCountWhenZero || !string.IsNullOrEmpty(count));
        }

        public async UniTask UpdateUIAsync(ItemSO item, int count)
        {
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

            _countText.text = count.ToShorthand();
            _countText.gameObject.SetActive(_showCountWhenZero || count > 0);
        }

        #endregion

        #region Private Methods

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
