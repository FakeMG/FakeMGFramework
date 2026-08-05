using FakeMG.Framework.EventBus;

namespace FakeMG.SaveLoad
{
    public struct RegisterDataRequesterEvent : IEvent
    {
        public DataRequester Requester;
    }

    public struct UnregisterDataRequesterEvent : IEvent
    {
        public DataRequester Requester;
    }
}
