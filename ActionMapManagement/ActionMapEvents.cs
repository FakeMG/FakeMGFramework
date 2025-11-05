using FakeMG.Framework.EventBus;

namespace FakeMG.Framework.ActionMapManagement
{
    public struct EnableActionMapEvent : IEvent
    {
        public string ActionMapName;
    }

    public struct DisableActionMapEvent : IEvent
    {
        public string ActionMapName;
    }
}
