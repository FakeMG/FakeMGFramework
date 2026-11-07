using FakeMG.Framework.EventBus;

namespace FakeMG.InputContexts
{
    public struct ShowInputContextEvent : IEvent
    {
        public IInputContext Context;
    }

    public struct HideInputContextEvent : IEvent
    {
        public IInputContext Context;
    }
}
