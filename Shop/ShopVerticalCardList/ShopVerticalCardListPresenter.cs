using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FakeMG.Shop.Config;
using FakeMG.Shop.Services.Purchase;
using UnityEngine;
using VContainer;

namespace FakeMG.Shop.UI
{
    //TODO: Could turn this into a non mono presenter
    public class ShopVerticalCardListPresenter : MonoBehaviour
    {
        [SerializeField] private ShopVerticalCardListView _shopVerticalCardListView;

        [Inject] private readonly ShopSession _shopSession;

        #region Unity Lifecycle

        private void OnEnable()
        {
            _shopVerticalCardListView.BuyPressed += TryPurchaseWhenBuyPressed;
            _shopSession.OnCatalogLoaded += RenderCatalogWhenCatalogLoads;
            _shopSession.OnListingHidden += HideListingWhenListingHides;
            BindCurrentCatalog();
        }

        private void OnDisable()
        {
            _shopVerticalCardListView.BuyPressed -= TryPurchaseWhenBuyPressed;
            _shopSession.OnCatalogLoaded -= RenderCatalogWhenCatalogLoads;
            _shopSession.OnListingHidden -= HideListingWhenListingHides;
        }

        #endregion

        #region Public Methods

        private void BindCurrentCatalog()
        {
            if (_shopSession == null)
            {
                return;
            }

            _shopVerticalCardListView.RenderCatalog(_shopSession.CurrentVisibleListingSOs);
        }

        #endregion

        #region Private Methods

        private void RenderCatalogWhenCatalogLoads(IReadOnlyList<ShopListingSO> shopListingSOs)
        {
            _shopVerticalCardListView.RenderCatalog(shopListingSOs);
        }

        private void HideListingWhenListingHides(string listingId)
        {
            _shopVerticalCardListView.HideListing(listingId);
        }

        private void TryPurchaseWhenBuyPressed(ShopListingSO shopListingSO)
        {
            _shopSession.TryPurchaseAsync(
                shopListingSO,
                this.GetCancellationTokenOnDestroy()).Forget();
        }

        #endregion
    }
}
