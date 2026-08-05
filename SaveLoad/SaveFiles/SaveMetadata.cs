using System;

namespace FakeMG.SaveLoad
{
    /// <summary>
    /// Identifies when and by which application version a managed save file was committed.
    /// </summary>
    [Serializable]
    public class SaveMetadata
    {
        public DateTime TimestampUtc;

        public string GameVersion;
        public SaveFileKind SaveKind;

        public DateTime GetTimestampUtc()
        {
            return TimestampUtc;
        }
    }
}
