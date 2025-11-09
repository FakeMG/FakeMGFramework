using FakeMG.Framework.EventBus;

namespace FakeMG.Framework.ActionMapManagement
{
    public struct EnableActionMapEvent : IEvent
    {
        public ActionMapSO ActionMap;
    }

    public struct DisableActionMapEvent : IEvent
    {
        public ActionMapSO ActionMap;
    }

    public struct PauseAllActionMapsEvent : IEvent { }

    public struct ResumeAllActionMapsEvent : IEvent { }
}