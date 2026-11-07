using FakeMG.Framework.EventBus;

namespace FakeMG.InputHints
{
    public struct AddInputHintContextEvent : IEvent
    {
        public IInputHintContext HintContext;
    }

    public struct RemoveInputHintContextEvent : IEvent
    {
        public IInputHintContext HintContext;
    }
}
