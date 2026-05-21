using FakeMG.Framework;

namespace FakeMG.Inventory
{
    public readonly struct InventoryChange
    {
        public readonly IdentitySO IdentitySO;
        public readonly int OldCount;
        public readonly int NewCount;

        public InventoryChange(IdentitySO identitySO, int oldCount, int newCount)
        {
            IdentitySO = identitySO;
            OldCount = oldCount;
            NewCount = newCount;
        }
    }
}
