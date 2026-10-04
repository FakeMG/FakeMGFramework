using System;
using System.Collections.Generic;
using FakeMG.Shop.Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FakeMG.Shop.UI
{
    public class ShopSectionView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private Image _headerImage;
        [SerializeField] private RectTransform _contentRootRectTransform;
        [SerializeField] private ShopListingCardView _shopListingCardViewPrefab;
        [SerializeField] private ShopBundleCardView _shopBundleCardViewPrefab;

        public event Action<ShopListingSO> BuyPressed;

        private readonly List<IShopListingCardView> _shopListingCardViews = new();
        private readonly List<MonoBehaviour> _spawnedCardBehaviours = new();

        #region Public Methods

        public void BindSection(ShopSectionViewDefinition shopSectionViewDefinition)
        {
            _titleText.text = shopSectionViewDefinition.Title;
            _headerImage.sprite = shopSectionViewDefinition.HeaderSprite;
            ClearListings();

            for (int listingIndex = 0; listingIndex < shopSectionViewDefinition.Listings.Count; listingIndex++)
            {
                ShopListingViewDefinition shopListingViewDefinition = shopSectionViewDefinition.Listings[listingIndex];
                IShopListingCardView shopListingCardView = SpawnCardView(shopListingViewDefinition);

                shopListingCardView.BindListing(shopListingViewDefinition);
                shopListingCardView.BuyPressed += ForwardBuyPressedWhenListingCardRequestsPurchase;

                _shopListingCardViews.Add(shopListingCardView);
            }
        }

        public void HideListing(string listingId)
        {
            foreach (IShopListingCardView shopListingCardView in _shopListingCardViews)
            {
                if (shopListingCardView.ListingId != listingId)
                {
                    continue;
                }

                shopListingCardView.SetVisible(false);
                return;
            }
        }

        public void ClearListings()
        {
            for (int presenterIndex = 0; presenterIndex < _shopListingCardViews.Count; presenterIndex++)
            {
                _shopListingCardViews[presenterIndex].BuyPressed -= ForwardBuyPressedWhenListingCardRequestsPurchase;
            }

            _shopListingCardViews.Clear();

            for (int behaviourIndex = 0; behaviourIndex < _spawnedCardBehaviours.Count; behaviourIndex++)
            {
                if (_spawnedCardBehaviours[behaviourIndex])
                {
                    Destroy(_spawnedCardBehaviours[behaviourIndex].gameObject);
                }
            }

            _spawnedCardBehaviours.Clear();
        }

        #endregion

        #region Private Methods

        //TODO: refactor to use factory pattern if more card types are added in the future
        private IShopListingCardView SpawnCardView(ShopListingViewDefinition shopListingViewDefinition)
        {
            MonoBehaviour cardBehaviour;
            if (shopListingViewDefinition.ListingSO is BundleListingSO)
            {
                cardBehaviour = Instantiate(_shopBundleCardViewPrefab, _contentRootRectTransform);
            }
            else
            {
                cardBehaviour = Instantiate(_shopListingCardViewPrefab, _contentRootRectTransform);
            }

            _spawnedCardBehaviours.Add(cardBehaviour);
            return (IShopListingCardView)cardBehaviour;
        }

        private void ForwardBuyPressedWhenListingCardRequestsPurchase(ShopListingSO shopListingSO)
        {
            BuyPressed?.Invoke(shopListingSO);
        }

        #endregion
    }
}
