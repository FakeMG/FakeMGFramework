namespace FakeMG.SaveLoad.Advanced
{
    public sealed class ManagedSaveFileInfo
    {
        public ManagedSaveFileInfo(string filePath, SaveMetadata metadata)
        {
            FilePath = SaveFileCatalog.NormalizeSavePath(filePath);
            FileName = SaveFileCatalog.GetSaveFileName(FilePath);
            RelativeFolderPath = SaveFileCatalog.GetRelativeFolderPath(FilePath);
            RelativeSavePath = SaveFileCatalog.GetRelativeSavePath(FilePath);
            Metadata = metadata;
        }

        public string FilePath { get; }

        public string FileName { get; }

        public string RelativeFolderPath { get; }

        public string RelativeSavePath { get; }

        public SaveMetadata Metadata { get; }
    }
}