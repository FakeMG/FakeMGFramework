using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FakeMG.Framework;
using UnityEditor;
using UnityEngine;

namespace FakeMG.SaveLoad.Editor
{
    internal sealed class SaveFileProtectionProfileResolver
    {
        private readonly List<string> _diagnostics = new();

        public IReadOnlyList<string> Diagnostics => _diagnostics;

        #region Public Methods

        public List<ValidatedSaveFileInfo> GetManagedSaveFiles()
        {
            _diagnostics.Clear();
            var managedFilesByPath = new Dictionary<string, ValidatedSaveFileInfo>(StringComparer.Ordinal);
            string[] protectionAssetGuids = AssetDatabase.FindAssets("t:SaveFileProtectionSO");

            foreach (string protectionAssetGuid in protectionAssetGuids)
            {
                string protectionAssetPath = AssetDatabase.GUIDToAssetPath(protectionAssetGuid);
                SaveFileProtectionSO protectionSO =
                    AssetDatabase.LoadAssetAtPath<SaveFileProtectionSO>(protectionAssetPath);
                if (!protectionSO)
                {
                    Echo.Error($"Save protection profile '{protectionAssetPath}' could not be loaded.");
                    continue;
                }

                try
                {
                    SaveFileProtectionSettings protectionSettings = protectionSO.CreateSettings();
                    var saveDataStore = new Es3SaveDataStore(
                        protectionSettings,
                        Application.persistentDataPath);
                    var saveFileCatalog = new SaveFileCatalog(
                        saveDataStore,
                        protectionSettings,
                        false,
                        new UnitySaveEnvironment());
                    foreach (ValidatedSaveFileInfo managedFile in saveFileCatalog.GetManagedSaveFiles())
                    {
                        managedFilesByPath.TryAdd(managedFile.SaveFilePath, managedFile);
                    }
                }
                catch (Exception exception)
                {
                    Echo.Error(
                        $"Save protection profile '{protectionAssetPath}' is invalid: {exception}");
                }
            }

            if (protectionAssetGuids.Length == 0)
            {
                Echo.Warning("Save File Viewer found no SaveFileProtectionSO assets.");
            }

            foreach (string candidateFilePath in DiscoverCanonicalCandidatePaths())
            {
                if (!managedFilesByPath.ContainsKey(candidateFilePath))
                {
                    _diagnostics.Add(
                        $"No configured save protection profile could validate '{candidateFilePath}'.");
                }
            }

            return new List<ValidatedSaveFileInfo>(managedFilesByPath.Values);
        }

        #endregion

        #region Private Methods

        private static IEnumerable<string> DiscoverCanonicalCandidatePaths()
        {
            string storageRootPath = Application.persistentDataPath;
            if (!Directory.Exists(storageRootPath))
            {
                return Array.Empty<string>();
            }

            List<string> candidatePaths = Directory
                .EnumerateFiles(storageRootPath, "*.json", SearchOption.TopDirectoryOnly)
                .Select(path => Path.GetFileName(path).Replace("\\", "/"))
                .Where(path => !SaveFileCatalog.IsTransactionCompanion(path))
                .ToList();
            string worldRootPath = Path.Combine(
                storageRootPath,
                SaveFileCatalog.WORLD_ROOT_DIRECTORY_PATH);
            if (!Directory.Exists(worldRootPath))
            {
                return candidatePaths;
            }

            foreach (string worldDirectoryPath in Directory.EnumerateDirectories(worldRootPath))
            {
                string worldId = Path.GetFileName(worldDirectoryPath);
                if (!WorldId.TryParse(worldId, out _))
                {
                    continue;
                }

                foreach (string absoluteFilePath in Directory.EnumerateFiles(worldDirectoryPath))
                {
                    string fileName = Path.GetFileName(absoluteFilePath);
                    string relativeFilePath = $"{SaveFileCatalog.WORLD_ROOT_DIRECTORY_PATH}/{worldId}/{fileName}";
                    if (!SaveFileCatalog.IsTransactionCompanion(relativeFilePath) &&
                        (string.Equals(
                             fileName,
                             SaveFileCatalog.WORLD_MANIFEST_FILE_NAME,
                             StringComparison.Ordinal) ||
                         SaveFileCatalog.IsWorldSnapshotPath(relativeFilePath)))
                    {
                        candidatePaths.Add(relativeFilePath);
                    }
                }
            }

            return candidatePaths;
        }

        #endregion
    }
}
