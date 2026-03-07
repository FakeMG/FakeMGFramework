using System;
using System.Collections.Generic;
using System.IO;

namespace FakeMG.SaveLoad.Advanced
{
    public static class SaveFileCatalog
    {
        public const string SAVE_FOLDER = "Saves/";
        public const string MANUAL_SAVE_PATH_PREFIX = "ManualSave_";
        public const string AUTO_SAVE_PATH_PREFIX = "AutoSave_";
        public const string METADATA_KEY = "Metadata";

        public static List<ManagedSaveFileInfo> GetManagedSaveFiles()
        {
            List<ManagedSaveFileInfo> saveFiles = new();
            string[] filePaths = GetFilesInSaveFolder();

            foreach (string filePath in filePaths)
            {
                if (!IsManagedSaveFile(filePath))
                {
                    continue;
                }

                if (!ES3.KeyExists(METADATA_KEY, filePath))
                {
                    continue;
                }

                try
                {
                    SaveMetadata metadata = ES3.Load(METADATA_KEY, filePath, new SaveMetadata());
                    saveFiles.Add(new ManagedSaveFileInfo(filePath, metadata));
                }
                catch (Exception)
                {
                    // Invalid metadata should not block access to other save files.
                }
            }

            return saveFiles;
        }

        public static string CreateManualSavePath(DateTime timestamp)
        {
            string manualSaveFileName = $"{MANUAL_SAVE_PATH_PREFIX}{timestamp.Ticks}";
            return NormalizeSavePath(manualSaveFileName);
        }

        public static string CreateAutoSavePath(DateTime timestamp)
        {
            string autoSaveFileName = $"{AUTO_SAVE_PATH_PREFIX}{timestamp.Ticks}";
            return NormalizeSavePath(autoSaveFileName);
        }

        public static string NormalizeSavePath(string savePath)
        {
            if (string.IsNullOrWhiteSpace(savePath))
            {
                return savePath;
            }

            string normalizedPath = savePath.Replace("\\", "/");
            if (normalizedPath.StartsWith(SAVE_FOLDER, StringComparison.Ordinal))
            {
                return normalizedPath;
            }

            return $"{SAVE_FOLDER}{normalizedPath}";
        }

        public static bool IsManagedSaveFile(string filePath)
        {
            string fileName = GetSaveFileName(filePath);

            return fileName.StartsWith(MANUAL_SAVE_PATH_PREFIX, StringComparison.Ordinal)
                   || fileName.StartsWith(AUTO_SAVE_PATH_PREFIX, StringComparison.Ordinal);
        }

        public static bool IsAutoSaveFile(string filePath)
        {
            string fileName = GetSaveFileName(filePath);
            return fileName.StartsWith(AUTO_SAVE_PATH_PREFIX, StringComparison.Ordinal);
        }

        public static string GetSaveFileName(string filePath)
        {
            string normalizedPath = filePath.Replace("\\", "/");
            return Path.GetFileNameWithoutExtension(normalizedPath);
        }

        private static string[] GetFilesInSaveFolder()
        {
            if (!ES3.DirectoryExists(SAVE_FOLDER))
            {
                return Array.Empty<string>();
            }

            string[] saveFiles = ES3.GetFiles(SAVE_FOLDER);
            for (int i = 0; i < saveFiles.Length; i++)
            {
                saveFiles[i] = NormalizeSavePath(saveFiles[i]);
            }

            return saveFiles;
        }
    }
}