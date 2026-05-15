using System;
using FakeMG.Framework.ExtensionMethods;
using FakeMG.Shop.Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FakeMG.Shop.UI
{
    public class ShopListingCardView : MonoBehaviour, IShopListingCardView
    {
        [SerializeField] private Button _buyButton;
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _amountText;
        [SerializeField] private TMP_Text _priceText;

        public event Action<ShopListingSO> BuyPressed;

        public string ListingId => _shopListingDefinition != null ? _shopListingDefinition.ListingId : string.Empty;

        private ShopListingViewDefinition _shopListingDefinition;

        #region Unity Lifecycle

        private void OnEnable()
        {
            _buyButton.onClick.AddListener(RaiseBuyPressedWhenBuyButtonPressed);
        }

        private void OnDisable()
        {
            _buyButton.onClick.RemoveListener(RaiseBuyPressedWhenBuyButtonPressed);
        }

        #endregion

        #region Public Methods

        public void BindListing(ShopListingViewDefinition shopListingViewDefinition)
        {
            _shopListingDefinition = shopListingViewDefinition;

            _iconImage.sprite = shopListingViewDefinition.ListingSO.ListingSprite;
            _amountText.text = string.Empty;

            foreach (var grantedItemEntry in shopListingViewDefinition.ListingSO.GetAllItemsGranted())
            {
                _amountText.text = grantedItemEntry.Value.SeparateNumberWithComma();
                break;
            }

            _priceText.text = ShopListingPriceTextBuilder.BuildPriceText(shopListingViewDefinition);
        }

        public void SetVisible(bool isVisible)
        {
            gameObject.SetActive(isVisible);
        }

        #endregion

        #region Private Methods

        private void RaiseBuyPressedWhenBuyButtonPressed()
        {
            if (_shopListingDefinition == null)
            {
                return;
            }

            BuyPressed?.Invoke(_shopListingDefinition.ListingSO);
        }

        #endregion
    }
}
