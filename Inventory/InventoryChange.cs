using FakeMG.Framework;
using FakeMG.Numbers;

namespace FakeMG.Inventory
{
    public readonly struct InventoryChange
    {
        public readonly IdentitySO IdentitySO;
        public readonly GameNumber OldCount;
        public readonly GameNumber NewCount;

        public InventoryChange(IdentitySO identitySO, GameNumber oldCount, GameNumber newCount)
        {
            IdentitySO = identitySO;
            OldCount = oldCount;
            NewCount = newCount;
        }
    }
}
