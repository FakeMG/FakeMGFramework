using FakeMG.Framework.Database;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.Shop.Config
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.SHOP + "/ShopListingDatabaseSO")]
    public class ShopListingDatabaseSO : DatabaseSO<ShopListingSO>
    {

    }
}
