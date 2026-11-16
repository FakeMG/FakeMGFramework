namespace FakeMG.GridSystem
{
    public enum PlacementChangeKind
    {
        Created,
        Moved,
        Removed,
        Replaced,
        Restored,
        Cleared,
        // Many structures committed at once; carries no single placement, so listeners refresh from the full set.
        BatchCreated
    }
}
