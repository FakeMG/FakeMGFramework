using System;

namespace FakeMG.Framework.EventBus
{
    public static class EventBus<T> where T : IEvent
    {
        public static event Action<T> OnEvent;
        public static event Action OnEventNoArgs;

        public static void Raise(T evt)
        {
            OnEvent?.Invoke(evt);
            OnEventNoArgs?.Invoke();
        }
    }
}