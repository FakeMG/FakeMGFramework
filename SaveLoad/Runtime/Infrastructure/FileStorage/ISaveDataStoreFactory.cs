namespace FakeMG.SaveLoad
{
    public interface ISaveDataStoreFactory
    {
        ISaveDataStore Create(ISaveDataStoreProfile profile);
    }
}
