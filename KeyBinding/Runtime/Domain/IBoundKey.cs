namespace FakeMG.KeyBinding
{
    /// <summary>A binding slot as conflict detection sees it: the control it uses and the action map it lives in.</summary>
    public interface IBoundKey
    {
        string ActionMapName { get; }
        string ControlPath { get; }
    }
}
