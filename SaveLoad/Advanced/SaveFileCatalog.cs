using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FakeMG.SaveLoad.Advanced
{
    public static class SaveFileCatalog
    {
        public const string ROOT_FOLDER_DISPLAY_NAME = "Root";
        public const string MANUAL_SAVE_PATH_PREFIX = "ManualSave_";
        public const string AUTO_SAVE_PATH_PREFIX = "AutoSave_";
        public const string METADATA_KEY = "Metadata";

        public static List<ManagedSaveFileInfo> GetManagedSaveFiles()
        {
            return GetManagedSaveFilesInFolder(string.Empty, true);
        }

        public static List<ManagedSaveFileInfo> GetManagedSaveFiles(string saveFolderPath)
        {
            string normalizedFolderPath = NormalizeSaveFolderPath(saveFolderPath);
            return GetManagedSaveFilesInFolder(normalizedFolderPath, false);
        }

        public static string CreateManualSavePath(string saveFolderPath, DateTime timestamp)
        {
            string manualSaveFileName = $"{MANUAL_SAVE_PATH_PREFIX}{timestamp.Ticks}";
            return NormalizeSavePath(manualSaveFileName, saveFolderPath);
        }

        public static string CreateAutoSavePath(string saveFolderPath, DateTime timestamp)
        {
            string autoSaveFileName = $"{AUTO_SAVE_PATH_PREFIX}{timestamp.Ticks}";
            return NormalizeSavePath(autoSaveFileName, saveFolderPath);
        }

        public static string NormalizeSaveFolderPath(string saveFolderPath)
        {
            return NormalizeFolderPath(saveFolderPath);
        }

        public static string NormalizeSavePath(string savePath)
        {
            return NormalizeSavePath(savePath, string.Empty);
        }

        public static string NormalizeSavePath(string savePath, string saveFolderPath)
        {
            if (string.IsNullOrWhiteSpace(savePath))
            {
                return savePath;
            }

            string normalizedPath = NormalizePathSeparators(savePath).Trim();
            if (Path.IsPathRooted(normalizedPath))
            {
                throw new ArgumentException("Save path must be relative.", nameof(savePath));
            }

            string trimmedPath = normalizedPath.Trim('/');
            ValidatePathSegments(trimmedPath, nameof(savePath), allowEmpty: false);

            string normalizedFolderPath = NormalizeFolderPath(saveFolderPath);

            if (HasDirectorySegments(trimmedPath))
            {
                ValidateFolderOwnership(trimmedPath, normalizedFolderPath, nameof(savePath));
                return trimmedPath;
            }

            return string.IsNullOrEmpty(normalizedFolderPath)
                ? trimmedPath
                : $"{normalizedFolderPath}/{trimmedPath}";
        }

        public static string GetRelativeFolderPath(string filePath)
        {
            string normalizedPath = NormalizeSavePath(filePath);
            int lastSeparatorIndex = normalizedPath.LastIndexOf('/');
            if (lastSeparatorIndex < 0)
            {
                return string.Empty;
            }

            return normalizedPath[..lastSeparatorIndex];
        }

        public static string GetRelativeSavePath(string filePath)
        {
            return NormalizeSavePath(filePath);
        }

        public static string GetFolderDisplayName(string relativeFolderPath)
        {
            return string.IsNullOrEmpty(relativeFolderPath)
                ? ROOT_FOLDER_DISPLAY_NAME
                : relativeFolderPath;
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
            string normalizedPath = NormalizePathSeparators(filePath);
            return Path.GetFileNameWithoutExtension(normalizedPath);
        }

        private static List<ManagedSaveFileInfo> GetManagedSaveFilesInFolder(string normalizedFolderPath, bool recursive)
        {
            List<ManagedSaveFileInfo> saveFiles = new();
            string[] filePaths = GetFilesInFolder(normalizedFolderPath, recursive);

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

        private static string NormalizeFolderPath(string saveFolderPath)
        {
            if (string.IsNullOrWhiteSpace(saveFolderPath))
            {
                return string.Empty;
            }

            string normalizedPath = NormalizePathSeparators(saveFolderPath).Trim().Trim('/');
            if (Path.IsPathRooted(normalizedPath))
            {
                throw new ArgumentException("Save folder path must be relative.", nameof(saveFolderPath));
            }

            ValidatePathSegments(normalizedPath, nameof(saveFolderPath), allowEmpty: true);
            return normalizedPath;
        }

        private static void ValidatePathSegments(string normalizedPath, string parameterName, bool allowEmpty)
        {
            string[] segments = normalizedPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (!allowEmpty && segments.Length == 0)
            {
                throw new ArgumentException("Path must contain at least one segment.", parameterName);
            }

            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] == "." || segments[i] == "..")
                {
                    throw new ArgumentException("Path cannot contain relative traversal segments.", parameterName);
                }

                if (segments[i].IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    throw new ArgumentException($"Path segment '{segments[i]}' contains invalid characters.", parameterName);
                }
            }
        }

        private static string[] GetFilesInFolder(string normalizedFolderPath, bool recursive)
        {
            if (!DirectoryExists(normalizedFolderPath))
            {
                return Array.Empty<string>();
            }

            List<string> filePaths = new();
            CollectFilesInFolder(normalizedFolderPath, recursive, filePaths);
            return filePaths.ToArray();
        }

        private static void CollectFilesInFolder(string normalizedFolderPath, bool recursive, ICollection<string> filePaths)
        {
            string[] saveFiles = GetFiles(normalizedFolderPath);
            for (int i = 0; i < saveFiles.Length; i++)
            {
                filePaths.Add(NormalizeSavePath(saveFiles[i], normalizedFolderPath));
            }

            if (!recursive)
            {
                return;
            }

            string[] subdirectories = GetDirectories(normalizedFolderPath);
            for (int i = 0; i < subdirectories.Length; i++)
            {
                string subdirectoryName = NormalizePathSeparators(subdirectories[i]).Trim('/');
                string subdirectoryPath = string.IsNullOrEmpty(normalizedFolderPath)
                    ? subdirectoryName
                    : $"{normalizedFolderPath}/{subdirectoryName}";
                CollectFilesInFolder(subdirectoryPath, true, filePaths);
            }
        }

        private static bool DirectoryExists(string normalizedFolderPath)
        {
            return string.IsNullOrEmpty(normalizedFolderPath)
                || ES3.DirectoryExists(normalizedFolderPath);
        }

        private static string[] GetFiles(string normalizedFolderPath)
        {
            if (!string.IsNullOrEmpty(normalizedFolderPath))
            {
                return ES3.GetFiles(normalizedFolderPath + "/");
            }

            string rootPath = Application.persistentDataPath;
            if (!Directory.Exists(rootPath))
            {
                return Array.Empty<string>();
            }

            string[] filePaths = Directory.GetFiles(rootPath);
            for (int i = 0; i < filePaths.Length; i++)
            {
                filePaths[i] = Path.GetFileName(filePaths[i]);
            }

            return filePaths;
        }

        private static string[] GetDirectories(string normalizedFolderPath)
        {
            if (!string.IsNullOrEmpty(normalizedFolderPath))
            {
                return ES3.GetDirectories(normalizedFolderPath);
            }

            string rootPath = Application.persistentDataPath;
            if (!Directory.Exists(rootPath))
            {
                return Array.Empty<string>();
            }

            string[] directoryPaths = Directory.GetDirectories(rootPath);
            for (int i = 0; i < directoryPaths.Length; i++)
            {
                directoryPaths[i] = Path.GetFileName(directoryPaths[i]);
            }

            return directoryPaths;
        }

        private static bool HasDirectorySegments(string normalizedPath)
        {
            return normalizedPath.Contains('/');
        }

        private static void ValidateFolderOwnership(string normalizedPath, string normalizedFolderPath, string parameterName)
        {
            if (string.IsNullOrEmpty(normalizedFolderPath))
            {
                return;
            }

            if (!normalizedPath.StartsWith(normalizedFolderPath + "/", StringComparison.Ordinal))
            {
                throw new ArgumentException("Save path must stay within the configured save folder.", parameterName);
            }
        }

        private static string NormalizePathSeparators(string path)
        {
            return path.Replace("\\", "/");
        }
    }
}
