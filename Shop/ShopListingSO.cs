using System.Collections.Generic;
using FakeMG.Framework;
using FakeMG.Inventory;
using FakeMG.Shop.RuntimeData;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FakeMG.Shop.Config
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.SHOP + "/ShopListingSO")]
    public class ShopListingSO : IdentitySO
    {
        [Title("Listing Info")]
        [SerializeField] private Sprite _listingSprite;
        [SerializeField] private List<ItemAmountEntry> _price = new();
        [SerializeField] private List<ItemAmountEntry> _itemsGranted = new();
        [SerializeField] private bool _isNonConsumable;
        [SerializeField] private ShopPurchaseType _purchaseType = ShopPurchaseType.InGameCurrency;

        #region Public Properties

        public Sprite ListingSprite => _listingSprite;
        public bool IsNonConsumable => _isNonConsumable;
        public ShopPurchaseType PurchaseType => _purchaseType;
        public string LocalizedIapPriceText { get; set; }

        #endregion

        #region Public Methods

        public IReadOnlyList<ItemAmountEntry> GetPrice()
        {
            return _price;
        }

        public virtual IReadOnlyList<ItemAmountEntry> GetAllItemsGranted()
        {
            return _itemsGranted;
        }

        #endregion
    }
}
