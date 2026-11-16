namespace FakeMG.GridSystem
{
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
