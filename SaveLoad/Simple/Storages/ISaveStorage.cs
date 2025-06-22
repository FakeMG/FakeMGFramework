namespace FakeMG.FakeMGFramework.SaveLoad.Simple.Storages
{
    public interface ISaveStorage
    {
        void Save(string saveId, SaveProfile profile);
        SaveProfile Load(string saveId);

        bool FileExists(string saveId);
    }
}