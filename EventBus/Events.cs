namespace FakeMG.Framework.EventBus
{
    public interface IEvent { }

    public struct ExampleEvent : IEvent { }

    public struct ExamplePlayerEvent : IEvent
    {
        public int Health;
        public int Mana;
    }

    // DO NOT ADD EVENTS HERE FOR YOUR PROJECT USE CASES
}