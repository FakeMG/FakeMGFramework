namespace FakeMG.InputContexts
{
    public interface IInputContext
    {
        /// <summary>Silences every context shown before this one, e.g. a dialog over its screen.</summary>
        bool IsModal { get; }

        InputActionHandlers Handlers { get; }
    }
}
