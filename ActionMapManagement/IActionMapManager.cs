namespace FakeMG.ActionMapManagement
{
    public interface IActionMapManager
    {
        bool IsActionMapActive(string mapName);
        void EnableActionMap(string mapName);
        void DisableActionMap(string mapName);
    }
}
