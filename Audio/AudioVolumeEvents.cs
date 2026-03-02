using FakeMG.Framework.EventBus;

namespace FakeMG.Audio
{
    public struct MasterVolumeChangedEvent : IEvent
    {
        public float Volume;
    }

    public struct MusicVolumeChangedEvent : IEvent
    {
        public float Volume;
    }

    public struct SfxVolumeChangedEvent : IEvent
    {
        public float Volume;
    }
}