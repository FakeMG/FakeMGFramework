using System;
using System.Collections.Generic;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.Shop.Config
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.SHOP + "/ShopVerticalCardListSO")]
    public class ShopVerticalCardListSO : ScriptableObject
    {
        [SerializeField] private List<ShopSectionViewDefinition> _sections = new();

        public IReadOnlyList<ShopSectionViewDefinition> Sections => _sections;
    }

    [Serializable]
    public class ShopSectionViewDefinition
    {
        [SerializeField] private string _title;
        [SerializeField] private Sprite _headerSprite;
        [SerializeField] private SectionLayoutType _layoutType;
        [SerializeField] private List<ShopListingViewDefinition> _listings = new();

        public string Title => _title;
        public Sprite HeaderSprite => _headerSprite;
        public SectionLayoutType LayoutType => _layoutType;
        public IReadOnlyList<ShopListingViewDefinition> Listings => _listings;

        #region Public Methods

        public ShopSectionViewDefinition CloneWithListings(IReadOnlyList<ShopListingViewDefinition> listings)
        {
            return new ShopSectionViewDefinition
            {
                _title = _title,
                _headerSprite = _headerSprite,
                _layoutType = _layoutType,
                _listings = new List<ShopListingViewDefinition>(listings),
            };
        }

        #endregion
    }
    public enum SectionLayoutType
    {
        Vertical,
        Grid
    }

    [Serializable]
    public class ShopListingViewDefinition
    {
        [SerializeField] private ShopListingSO _listing;
        [SerializeField] private BundleVisualSO _bundleVisual;

        public ShopListingSO ListingSO => _listing;
        public string ListingId => _listing != null ? _listing.Id : string.Empty;
        public BundleVisualSO BundleVisual => _bundleVisual;
    }
}
