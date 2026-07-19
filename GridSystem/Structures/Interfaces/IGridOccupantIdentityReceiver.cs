namespace FakeMG.GridSystem
{
    /// <summary>
    /// Receives the runtime placement identity after a structure prefab is instantiated.
    /// </summary>
    public interface IGridOccupantIdentityReceiver
    {
        void SetGridOccupantIdentity(GridOccupantIdentity structureInstanceIdentity);
    }
}
