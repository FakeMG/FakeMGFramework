namespace FakeMG.GridSystem
{
    /// <summary>
    /// Event payload for a committed structure placement change.
    /// </summary>
    public readonly struct PlacementChange
    {
        public PlacementChange(
            PlacementChangeKind kind,
            GridOccupantPlacement structurePlacement,
            string instanceId)
        {
            Kind = kind;
            GridOccupantPlacement = structurePlacement;
            InstanceId = instanceId;
        }

        public PlacementChangeKind Kind { get; }
        public GridOccupantPlacement GridOccupantPlacement { get; }
        public string InstanceId { get; }
    }
}
