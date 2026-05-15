using System.Collections.Generic;
using FakeMG.Framework;
using FakeMG.Shop.RuntimeData;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace FakeMG.Shop.Config
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.SHOP + "/ShopListingSO")]
    public class ShopListingSO : IdentitySO
    {
        [Title("Listing Info")]
        [SerializeField] private Sprite _listingSprite;
        [OdinSerialize] private Dictionary<IdentitySO, int> _price = new();
        [OdinSerialize] private Dictionary<IdentitySO, int> _itemsGranted = new();
        [SerializeField] private bool _isNonConsumable;
        [SerializeField] private ShopPurchaseType _purchaseType = ShopPurchaseType.InGameCurrency;

        #region Public Properties

        public Sprite ListingSprite => _listingSprite;
        public bool IsNonConsumable => _isNonConsumable;
        public ShopPurchaseType PurchaseType => _purchaseType;
        public string LocalizedIapPriceText { get; set; }

        #endregion

        #region Public Methods

        public IReadOnlyDictionary<IdentitySO, int> GetPrice()
        {
            return _price;
        }

        public virtual IReadOnlyDictionary<IdentitySO, int> GetAllItemsGranted()
        {
            return _itemsGranted;
        }

        #endregion
    }
}
