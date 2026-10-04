using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FakeMG.Shop.UI
{
    public class ShopVerticalCardListLifetimeScope : LifetimeScope
    {
        [SerializeField] private ShopVerticalCardListPresenter _shopVerticalCardListPresenter;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(_shopVerticalCardListPresenter);
        }
    }
}
