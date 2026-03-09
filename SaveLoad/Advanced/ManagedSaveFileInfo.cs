namespace FakeMG.SaveLoad.Advanced
{
    public sealed class ManagedSaveFileInfo
    {
        public ManagedSaveFileInfo(string saveFilePath, SaveMetadata metadata)
        {
            SaveFilePath = SaveFileCatalog.NormalizeSaveFilePath(saveFilePath);
            SaveFileName = SaveFileCatalog.GetSaveFileName(SaveFilePath);
            SaveDirectoryPath = SaveFileCatalog.GetSaveDirectoryPath(SaveFilePath);
            Metadata = metadata;
            SaveKind = Metadata.SaveKind;
        }

        public string SaveFilePath { get; }

        public string SaveFileName { get; }

        public string SaveDirectoryPath { get; }

        public SaveMetadata Metadata { get; }

        public SaveFileKind SaveKind { get; }
    }
}