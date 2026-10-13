using Cysharp.Threading.Tasks;
using FakeMG.Framework.ExtensionMethods;
using FakeMG.Numbers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FakeMG.Framework.UI
{
    public class ItemIconUIUpdater : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _countText;
        [SerializeField] private bool _showCountWhenZero;
        [Tooltip("Keep loaded icons until this view is destroyed. Useful for previews with repeated selections.")]
        [SerializeField] private bool _shouldCacheIcons;

        private readonly AddressableIconCache _iconCache = new();
        private int _updateRequestVersion;

        #region Unity Lifecycle

        private void OnDestroy()
        {
            InvalidatePendingRequests();
            _iconCache.Dispose();
        }

        #endregion

        #region Public Methods

        public void UpdateUI(Sprite newIcon, int count)
        {
            SetIcon(newIcon);
            UpdateCount(count);
        }

        public void UpdateUI(Sprite newIcon, string count)
        {
            SetIcon(newIcon);
            ApplyCountPresentation(
                count,
                _showCountWhenZero || !string.IsNullOrEmpty(count));
        }

        public void UpdateCount(int count)
        {
            ApplyCountPresentation(
                count > 0 ? count.ToShorthand(3) : "0",
                _showCountWhenZero || count > 0);
        }

        public void UpdateCount(GameNumber count)
        {
            ApplyCountPresentation(
                count > GameNumber.Zero ? count.ToShorthand(3) : "0",
                _showCountWhenZero || count > GameNumber.Zero);
        }

        public async UniTask UpdateUIAsync(IdentitySO item, GameNumber count)
        {
            await UpdateUIAsync(
                item,
                count > GameNumber.Zero ? count.ToShorthand(3) : "0",
                _showCountWhenZero || count > GameNumber.Zero);
        }

        public async UniTask UpdateUIAsync(IdentitySO item, int count)
        {
            await UpdateUIAsync(
                item,
                count.SeparateNumberWithComma(),
                _showCountWhenZero || count > 0);
        }

        public async UniTask UpdateUIAsync(IdentitySO item, string countText, bool isCountVisible)
        {
            int requestVersion = InvalidatePendingRequests();
            if (!_shouldCacheIcons)
            {
                _iconCache.Clear();
            }

            ApplyCountPresentation(countText, isCountVisible);
            ApplyIconPresentation(null);
            Sprite sprite = await _iconCache.LoadIconAsync(item, this.GetCancellationTokenOnDestroy());
            if (requestVersion == _updateRequestVersion)
            {
                ApplyIconPresentation(sprite);
            }
        }

        public void ClearUI()
        {
            InvalidatePendingRequests();
            if (!_shouldCacheIcons)
            {
                _iconCache.Clear();
            }

            ApplyIconPresentation(null);
            ApplyCountPresentation(string.Empty, false);
        }

        #endregion

        #region Private Methods

        private void SetIcon(Sprite newIcon)
        {
            InvalidatePendingRequests();
            if (!_shouldCacheIcons)
            {
                _iconCache.Clear();
            }

            ApplyIconPresentation(newIcon);
        }

        private void ApplyIconPresentation(Sprite newIcon)
        {
            _icon.sprite = newIcon;
            _icon.enabled = newIcon != null;
        }

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

        #endregion
    }
}
