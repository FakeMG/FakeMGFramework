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

        private void OnDestroy()
        {
            UnloadHandle();
        }

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

            // Load the sprite asynchronously
            if (item.IconSpriteAsset != null && item.IconSpriteAsset.RuntimeKeyIsValid())
            {
                var spriteHandle = Addressables.LoadAssetAsync<Sprite>(item.IconSpriteAsset);
                await spriteHandle;

                if (spriteHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    _icon.sprite = spriteHandle.Result;
                    _loadedSpriteHandle = spriteHandle;
                }
            }

            _countText.text = count > 0 ? count.ToShorthand() : string.Empty;
            _countText.gameObject.SetActive(_showCountWhenZero || count > 0);
        }

        private void UnloadHandle()
        {
            if (_loadedSpriteHandle.HasValue && _loadedSpriteHandle.Value.IsValid())
            {
                Addressables.Release(_loadedSpriteHandle.Value);
                _loadedSpriteHandle = null;
            }
        }
    }
}