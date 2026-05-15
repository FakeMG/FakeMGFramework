using System;
using System.Collections.Generic;
using System.Globalization;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using FakeMG.Framework.ExtensionMethods;
using FakeMG.Framework.UI;
using FakeMG.Shop.Config;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace FakeMG.Shop.UI
{
    public class ShopBundleCardView : MonoBehaviour, IShopListingCardView
    {
        [Title("Bundle Info UI")]
        [SerializeField] private Button _buyButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Image _bundleIconImage;
        [SerializeField] private Image _auraImage;
        [SerializeField] private Image _bundleBackgroundImage;
        [SerializeField] private Image _headerBackgroundImage;
        [SerializeField] private TMP_Text _bundleNameText;
        [SerializeField] private TMP_Text _priceText;

        [Title("Currency UI")]
        [SerializeField] private Image _currencyIconImage;
        [SerializeField] private TMP_Text _currencyAmountText;

        [Title("Booster Items Root")]
        [SerializeField] private RectTransform _boosterItemsRootRectTransform;
        [SerializeField] private ItemIconUIUpdater _itemIconUiUpdaterPrefab;

        [Title("Special Item Icons")]
        [SerializeField] private IdentitySO _infiniteHeartItemSO;
        [SerializeField] private GameObject _infiniteHeartIcon;
        [SerializeField] private TMP_Text _infiniteHeartDurationText;
        [SerializeField] private TMP_Text _infiniteHeartDurationText2;
        [SerializeField] private Transform _infiniteHeartDurationText2Parent;
        [SerializeField] private IdentitySO _noAdsItemSO;
        [SerializeField] private GameObject _noAdsIcon;

        public event Action<ShopListingSO> BuyPressed;
        public event Action ClosePressed;

        private ShopListingViewDefinition _shopListingViewDefinition;
        private AsyncOperationHandle<Sprite>? _currencyIconHandle;
        private int _currencyIconLoadVersion;

        public string ListingId => _shopListingViewDefinition != null ? _shopListingViewDefinition.ListingId : string.Empty;

        #region Unity Lifecycle

        private void OnEnable()
        {
            _buyButton.onClick.AddListener(RaiseBuyPressedWhenBuyButtonPressed);

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(RaiseClosePressedWhenCloseButtonPressed);
            }
        }

        private void OnDisable()
        {
            _buyButton.onClick.RemoveListener(RaiseBuyPressedWhenBuyButtonPressed);

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(RaiseClosePressedWhenCloseButtonPressed);
                _closeButton.interactable = true;
            }
        }

        private void OnDestroy()
        {
            ReleaseCurrencyIconHandle();
        }

        #endregion

        #region Public Methods

        public void BindListing(ShopListingViewDefinition shopListingViewDefinition)
        {
            _shopListingViewDefinition = shopListingViewDefinition;

            ClearRoots();
            HideSpecialItemIcons();

            if (shopListingViewDefinition.ListingSO is BundleListingSO bundleListingSo)
            {
                SpawnBoosterItems(bundleListingSo);
                UpdateSpecialItemIcons(bundleListingSo);
                UpdateCurrencyUI(bundleListingSo).Forget();
                BundleVisualSO bundleVisual = shopListingViewDefinition.BundleVisual;
                _bundleBackgroundImage.sprite = bundleVisual.BundleBackgroundSprite;
                _headerBackgroundImage.sprite = bundleVisual.HeaderBackgroundSprite;
                _auraImage.sprite = bundleVisual.AuraSprite;
            }
            else
            {
                _bundleBackgroundImage.sprite = null;
                _headerBackgroundImage.sprite = null;
                _auraImage.sprite = null;
                HideCurrencyUI();
            }

            _bundleNameText.text = shopListingViewDefinition.ListingSO.ItemName;
            _bundleIconImage.sprite = shopListingViewDefinition.ListingSO.ListingSprite;
            _priceText.text = ShopListingPriceTextBuilder.BuildPriceText(shopListingViewDefinition);
        }

        public void SetVisible(bool isVisible)
        {
            gameObject.SetActive(isVisible);
        }

        #endregion

        #region Private Methods

        private void ClearRoots()
        {
            foreach (Transform child in _boosterItemsRootRectTransform)
            {
                Destroy(child.gameObject);
            }
        }

        private void SpawnBoosterItems(BundleListingSO bundleListingSo)
        {
            foreach (var kvp in bundleListingSo.BoostersGranted)
            {
                SpawnItemIcon(kvp.Key, kvp.Value, _boosterItemsRootRectTransform);
            }
        }

        private void SpawnItemIcon(IdentitySO item, int count, RectTransform root)
        {
            var updater = Instantiate(_itemIconUiUpdaterPrefab, root);
            updater.UpdateUIAsync(item, count).Forget();
        }

        private void HideCurrencyUI()
        {
            _currencyIconImage.sprite = null;
            _currencyIconImage.gameObject.SetActive(false);
            _currencyAmountText.gameObject.SetActive(false);
        }

        private void HideSpecialItemIcons()
        {
            _infiniteHeartIcon.SetActive(false);
            _infiniteHeartDurationText.text = string.Empty;
            _infiniteHeartDurationText2.text = string.Empty;
            _noAdsIcon.SetActive(false);
        }

        private void UpdateSpecialItemIcons(BundleListingSO bundleListingSo)
        {
            IReadOnlyDictionary<IdentitySO, int> grantedItemsByItem = bundleListingSo.GetAllItemsGranted();

            bool hasInfiniteHeart = grantedItemsByItem.TryGetValue(_infiniteHeartItemSO, out int infiniteHeartAmount) &&
                                    infiniteHeartAmount > 0;
            bool hasNoAds = grantedItemsByItem.TryGetValue(_noAdsItemSO, out int noAdsAmount) &&
                            noAdsAmount > 0;

            _infiniteHeartIcon.SetActive(hasInfiniteHeart);
            _infiniteHeartDurationText.text = _infiniteHeartDurationText2.text = hasInfiniteHeart
                ? BuildInfiniteHeartDurationText(infiniteHeartAmount)
                : string.Empty;
            _infiniteHeartDurationText2Parent.gameObject.SetActive(hasInfiniteHeart && !hasNoAds);
            _infiniteHeartDurationText.gameObject.SetActive(hasInfiniteHeart && hasNoAds);

            _noAdsIcon.SetActive(hasNoAds);
        }

        private static string BuildInfiniteHeartDurationText(int durationSeconds)
        {
            float durationHours = Mathf.Max(0, durationSeconds) / 3600f;
            return $"{durationHours.ToString("0.#", CultureInfo.InvariantCulture)}h";
        }

        private async UniTask UpdateCurrencyUI(BundleListingSO bundleListingSo)
        {
            ReleaseCurrencyIconHandle();

            if (bundleListingSo.CurrencyItemGranted == null || bundleListingSo.CurrencyAmountGranted <= 0)
            {
                HideCurrencyUI();
                return;
            }

            _currencyAmountText.text = bundleListingSo.CurrencyAmountGranted.SeparateNumberWithComma();
            _currencyIconImage.gameObject.SetActive(true);
            _currencyAmountText.gameObject.SetActive(true);

            if (bundleListingSo.CurrencyItemGranted.IconSpriteAsset != null && bundleListingSo.CurrencyItemGranted.IconSpriteAsset.RuntimeKeyIsValid())
            {
                int loadVersion = ++_currencyIconLoadVersion;
                AsyncOperationHandle<Sprite> spriteHandle =
                    Addressables.LoadAssetAsync<Sprite>(bundleListingSo.CurrencyItemGranted.IconSpriteAsset);

                try
                {
                    Sprite sprite = await spriteHandle.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());

                    if (spriteHandle.Status != AsyncOperationStatus.Succeeded)
                    {
                        Echo.Error($"Failed to load currency icon for bundle listing '{bundleListingSo.name}'.");
                        Addressables.Release(spriteHandle);
                        return;
                    }

                    if (loadVersion != _currencyIconLoadVersion)
                    {
                        Addressables.Release(spriteHandle);
                        return;
                    }

                    if (!_currencyIconImage)
                    {
                        Addressables.Release(spriteHandle);
                        return;
                    }

                    _currencyIconImage.sprite = sprite;
                    _currencyIconHandle = spriteHandle;
                }
                catch (OperationCanceledException)
                {
                    if (spriteHandle.IsValid())
                    {
                        Addressables.Release(spriteHandle);
                    }
                }
            }
            else
            {
                Echo.Error($"Invalid currency icon reference for bundle listing '{bundleListingSo.name}'.");
            }
        }

        private void ReleaseCurrencyIconHandle()
        {
            if (!_currencyIconHandle.HasValue || !_currencyIconHandle.Value.IsValid())
            {
                return;
            }

            Addressables.Release(_currencyIconHandle.Value);
            _currencyIconHandle = null;
        }

        private void RaiseBuyPressedWhenBuyButtonPressed()
        {
            if (_shopListingViewDefinition == null)
            {
                return;
            }

            BuyPressed?.Invoke(_shopListingViewDefinition.ListingSO);
        }

        private void RaiseClosePressedWhenCloseButtonPressed()
        {
            _closeButton.interactable = false;
            ClosePressed?.Invoke();
        }

        #endregion
    }
}
