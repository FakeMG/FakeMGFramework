using System;

namespace FakeMG.SaveLoad
{
    [Serializable]
    public class SaveMetadata
    {
        public DateTime TimestampUtc;
        public string ApplicationVersion;
        public SaveFileKind SaveKind;
        public string OwnerId;
    }
}
