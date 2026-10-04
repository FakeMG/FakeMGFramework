using System;
using System.Collections.Generic;
using FakeMG.Framework.UI.Popup;
using FakeMG.Shop.Config;
using UnityEngine;
using UnityEngine.UI;

namespace FakeMG.Shop.UI
{
    public class ShopVerticalCardListView : PopupFadeAnimator
    {
        [SerializeField] private Button _closeButton;
        [SerializeField] private ShopVerticalCardListSO _shopCatalogSO;
        [SerializeField] private RectTransform _sectionsRootRectTransform;
        [SerializeField] private ShopSectionView _shopSectionVerticalViewPrefab;
        [SerializeField] private ShopSectionView _shopSectionGridViewPrefab;

        public event Action ClosePressed;
        public event Action<ShopListingSO> BuyPressed;

        private readonly List<ShopSectionView> _shopSectionViews = new();

        #region Unity Lifecycle

        private void OnEnable()
        {
            _closeButton.onClick.AddListener(RaiseClosePressedWhenCloseButtonPressed);
        }

        private void OnDisable()
        {
            _closeButton.onClick.RemoveListener(RaiseClosePressedWhenCloseButtonPressed);
            _closeButton.interactable = true;
        }

        #endregion

        #region Public Methods

        public void RenderCatalog(IReadOnlyList<ShopListingSO> filteredShopListingSOs)
        {
            ClearSections();

            if (filteredShopListingSOs == null)
            {
                return;
            }

            HashSet<ShopListingSO> visibleListingSOs = BuildVisibleListingSOSet(filteredShopListingSOs);

            foreach (ShopSectionViewDefinition section in _shopCatalogSO.Sections)
            {
                if (section == null)
                {
                    continue;
                }

                ShopSectionViewDefinition visibleSection = BuildVisibleSection(section, visibleListingSOs);
                if (visibleSection.Listings.Count == 0)
                {
                    continue;
                }

                ShopSectionView shopSectionView = visibleSection.LayoutType == SectionLayoutType.Grid
                    ? Instantiate(_shopSectionGridViewPrefab, _sectionsRootRectTransform)
                    : Instantiate(_shopSectionVerticalViewPrefab, _sectionsRootRectTransform);

                shopSectionView.BindSection(visibleSection);
                shopSectionView.BuyPressed += ForwardBuyPressedWhenSectionBuyPressed;
                _shopSectionViews.Add(shopSectionView);
            }
        }

        public void HideListing(string listingId)
        {
            foreach (ShopSectionView shopSectionView in _shopSectionViews)
            {
                shopSectionView.HideListing(listingId);
            }
        }

        public void ClearSections()
        {
            foreach (ShopSectionView shopSectionView in _shopSectionViews)
            {
                shopSectionView.BuyPressed -= ForwardBuyPressedWhenSectionBuyPressed;
                if (shopSectionView)
                {
                    Destroy(shopSectionView.gameObject);
                }
            }

            _shopSectionViews.Clear();
        }

        #endregion

        #region Private Methods

        private static HashSet<ShopListingSO> BuildVisibleListingSOSet(IReadOnlyList<ShopListingSO> filteredShopListingSOs)
        {
            var visibleListingSOs = new HashSet<ShopListingSO>();
            foreach (ShopListingSO shopListingSO in filteredShopListingSOs)
            {
                if (shopListingSO == null)
                {
                    continue;
                }

                visibleListingSOs.Add(shopListingSO);
            }

            return visibleListingSOs;
        }

        private static ShopSectionViewDefinition BuildVisibleSection(ShopSectionViewDefinition section, HashSet<ShopListingSO> visibleListingSOs)
        {
            var visibleListings = new List<ShopListingViewDefinition>();
            foreach (ShopListingViewDefinition shopListingViewDefinition in section.Listings)
            {
                if (shopListingViewDefinition?.ListingSO == null || !visibleListingSOs.Contains(shopListingViewDefinition.ListingSO))
                {
                    continue;
                }

                visibleListings.Add(shopListingViewDefinition);
            }

            return section.CloneWithListings(visibleListings);
        }

        private void ForwardBuyPressedWhenSectionBuyPressed(ShopListingSO shopListingSO)
        {
            BuyPressed?.Invoke(shopListingSO);
        }

        private void RaiseClosePressedWhenCloseButtonPressed()
        {
            _closeButton.interactable = false;
            ClosePressed?.Invoke();
        }

        #endregion
    }
}
