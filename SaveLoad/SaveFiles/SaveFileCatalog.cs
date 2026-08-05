using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.SaveLoad
{
    /// <summary>
    /// Discovers managed save files and owns their validated relative-path conventions. Durable
    /// reads are delegated to the configured store so catalog behavior is serialization-agnostic.
    /// </summary>
    public sealed class SaveFileCatalog
    {
        public const string ROOT_FOLDER_DISPLAY_NAME = "Root";
        public const string DEFAULT_FIXED_SAVE_FILE_NAME = "Settings";
        public const string MANUAL_SAVE_PATH_PREFIX = "ManualSave_";
        public const string AUTO_SAVE_PATH_PREFIX = "AutoSave_";
        public const string METADATA_KEY = "Metadata";

        private readonly ISaveDataStore _saveDataStore;

        public SaveFileCatalog(ISaveDataStore saveDataStore)
        {
            _saveDataStore = saveDataStore;
        }

        public List<ManagedSaveFileInfo> GetManagedSaveFiles()
        {
            return GetManagedSaveFilesInDirectory(string.Empty, true);
        }

        public List<ManagedSaveFileInfo> GetManagedSaveFiles(string saveDirectoryPath)
        {
            string normalizedDirectoryPath = NormalizeSaveDirectoryPath(saveDirectoryPath);
            return GetManagedSaveFilesInDirectory(normalizedDirectoryPath, false);
        }

        public static string CreateManualSaveFilePath(string saveDirectoryPath, DateTime timestamp)
        {
            string manualSaveFileName = $"{MANUAL_SAVE_PATH_PREFIX}{timestamp.Ticks}";
            return NormalizeSaveFilePath(manualSaveFileName, saveDirectoryPath);
        }

        public static string CreateAutoSaveFilePath(string saveDirectoryPath, DateTime timestamp)
        {
            string autoSaveFileName = $"{AUTO_SAVE_PATH_PREFIX}{timestamp.Ticks}";
            return NormalizeSaveFilePath(autoSaveFileName, saveDirectoryPath);
        }

        public static string CreateFixedSaveFilePath(string saveDirectoryPath, string saveFileName)
        {
            return NormalizeSaveFilePath(saveFileName, saveDirectoryPath);
        }

        public static string NormalizeSaveFilePath(string saveFilePath)
        {
            return NormalizeSaveFilePath(saveFilePath, string.Empty);
        }

        public static string NormalizeSaveFilePath(string saveFilePath, string saveDirectoryPath)
        {
            if (string.IsNullOrWhiteSpace(saveFilePath))
            {
                return saveFilePath;
            }

            string normalizedPath = NormalizePathSeparators(saveFilePath).Trim();
            if (Path.IsPathRooted(normalizedPath))
            {
                throw new ArgumentException("Save file path must be relative.", nameof(saveFilePath));
            }

            string trimmedPath = normalizedPath.Trim('/');
            ValidatePathSegments(trimmedPath, nameof(saveFilePath), allowEmpty: false);

            string normalizedDirectoryPath = NormalizeDirectoryPath(saveDirectoryPath);

            if (HasDirectorySegments(trimmedPath))
            {
                ValidateDirectoryOwnership(trimmedPath, normalizedDirectoryPath, nameof(saveFilePath));
                return trimmedPath;
            }

            return string.IsNullOrEmpty(normalizedDirectoryPath)
                ? trimmedPath
                : $"{normalizedDirectoryPath}/{trimmedPath}";
        }

        public static string NormalizeSaveDirectoryPath(string saveDirectoryPath)
        {
            return NormalizeDirectoryPath(saveDirectoryPath);
        }

        private static string NormalizeDirectoryPath(string saveDirectoryPath)
        {
            if (string.IsNullOrWhiteSpace(saveDirectoryPath))
            {
                return string.Empty;
            }

            string normalizedPath = NormalizePathSeparators(saveDirectoryPath).Trim().Trim('/');
            if (Path.IsPathRooted(normalizedPath))
            {
                throw new ArgumentException("Save directory path must be relative.", nameof(saveDirectoryPath));
            }

            ValidatePathSegments(normalizedPath, nameof(saveDirectoryPath), allowEmpty: true);
            return normalizedPath;
        }

        public static string GetSaveDirectoryPath(string saveFilePath)
        {
            string normalizedPath = NormalizeSaveFilePath(saveFilePath);
            int lastSeparatorIndex = normalizedPath.LastIndexOf('/');
            if (lastSeparatorIndex < 0)
            {
                return string.Empty;
            }

            return normalizedPath[..lastSeparatorIndex];
        }

        public static string GetSaveKindBadge(SaveMetadata metadata)
        {
            SaveFileKind saveKind = metadata.SaveKind;

            return saveKind switch
            {
                SaveFileKind.Auto => "[Auto]",
                SaveFileKind.Fixed => "[Fixed]",
                _ => "[Manual]",
            };
        }

        public static string GetSaveFileName(string saveFilePath)
        {
            string normalizedPath = NormalizePathSeparators(saveFilePath);
            return Path.GetFileNameWithoutExtension(normalizedPath);
        }

        private List<ManagedSaveFileInfo> GetManagedSaveFilesInDirectory(string normalizedDirectoryPath, bool recursive)
        {
            List<ManagedSaveFileInfo> saveFiles = new();
            string[] saveFilePaths = GetSaveFilePathsInDirectory(normalizedDirectoryPath, recursive);

            foreach (string saveFilePath in saveFilePaths)
            {
                if (TryLoadManagedMetadata(saveFilePath, out SaveMetadata metadata))
                {
                    saveFiles.Add(new ManagedSaveFileInfo(saveFilePath, metadata));
                }
            }

            return saveFiles;
        }

        private bool TryLoadManagedMetadata(string saveFilePath, out SaveMetadata metadata)
        {
            metadata = null;

            try
            {
                if (!_saveDataStore.KeyExists(METADATA_KEY, saveFilePath))
                {
                    return false;
                }

                metadata = _saveDataStore.LoadMetadata(saveFilePath);
                return metadata.SaveKind != SaveFileKind.Unknown;
            }
            catch (Exception)
            {
                // Invalid or incompatible files should not block discovery of valid save files.
                LogSkippedManagedFileIfEditor(saveFilePath, "Failed to read managed metadata.");
                return false;
            }
        }

        [Conditional("UNITY_EDITOR")]
        private static void LogSkippedManagedFileIfEditor(string saveFilePath, string reason)
        {
            Echo.Warning($"[SaveFileCatalog] Skipped '{saveFilePath}' while scanning managed save files. {reason}");
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

        private string[] GetSaveFilePathsInDirectory(string normalizedDirectoryPath, bool recursive)
        {
            if (!DirectoryExists(normalizedDirectoryPath))
            {
                return Array.Empty<string>();
            }

            List<string> saveFilePaths = new();
            CollectSaveFilePaths(normalizedDirectoryPath, recursive, saveFilePaths);
            return saveFilePaths.ToArray();
        }

        private void CollectSaveFilePaths(string normalizedDirectoryPath, bool recursive, ICollection<string> saveFilePaths)
        {
            string[] saveFiles = GetFiles(normalizedDirectoryPath);
            for (int i = 0; i < saveFiles.Length; i++)
            {
                saveFilePaths.Add(NormalizeSaveFilePath(saveFiles[i], normalizedDirectoryPath));
            }

            if (!recursive)
            {
                return;
            }

            string[] subdirectories = GetDirectories(normalizedDirectoryPath);
            for (int i = 0; i < subdirectories.Length; i++)
            {
                string subdirectoryName = NormalizePathSeparators(subdirectories[i]).Trim('/');
                string subdirectoryPath = string.IsNullOrEmpty(normalizedDirectoryPath)
                    ? subdirectoryName
                    : $"{normalizedDirectoryPath}/{subdirectoryName}";
                CollectSaveFilePaths(subdirectoryPath, true, saveFilePaths);
            }
        }

        private bool DirectoryExists(string normalizedDirectoryPath)
        {
            return _saveDataStore.DirectoryExists(normalizedDirectoryPath);
        }

        private string[] GetFiles(string normalizedDirectoryPath)
        {
            string storeDirectoryPath = string.IsNullOrEmpty(normalizedDirectoryPath)
                ? string.Empty
                : normalizedDirectoryPath + "/";
            return _saveDataStore.GetFiles(storeDirectoryPath);
        }

        private string[] GetDirectories(string normalizedDirectoryPath)
        {
            return _saveDataStore.GetDirectories(normalizedDirectoryPath);
        }

        private static bool HasDirectorySegments(string normalizedPath)
        {
            return normalizedPath.Contains('/');
        }

        private static void ValidateDirectoryOwnership(string normalizedPath, string normalizedDirectoryPath, string parameterName)
        {
            if (string.IsNullOrEmpty(normalizedDirectoryPath))
            {
                return;
            }

            if (!normalizedPath.StartsWith(normalizedDirectoryPath + "/", StringComparison.Ordinal))
            {
                throw new ArgumentException("Save file path must stay within the configured save directory.", parameterName);
            }
        }

        private static string NormalizePathSeparators(string path)
        {
            return path.Replace("\\", "/");
        }
    }
}
