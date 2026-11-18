namespace FakeMG.Settings
{
    public interface ISettingRow
    {
        string Title { get; }
        string Description { get; }

        void ShowFocused();
        void ShowUnfocused();
    }
}
