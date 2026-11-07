namespace FakeMG.InputContexts
{
    /// <summary>One screen, popup or gameplay mode that handles input while it is on screen.</summary>
    public interface IInputContext
    {
        /// <summary>Silences every context shown before this one, e.g. a dialog over its screen.</summary>
        bool IsModal { get; }

        InputActionHandlers Handlers { get; }
    }
}
