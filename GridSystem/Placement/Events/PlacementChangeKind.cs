namespace FakeMG.GridSystem
{
    /// <summary>
    /// Describes the committed operation that changed runtime placement state.
    /// </summary>
    public enum PlacementChangeKind
    {
        Created,
        Moved,
        Removed,
        Replaced,
        Restored,
        Cleared
    }
}
