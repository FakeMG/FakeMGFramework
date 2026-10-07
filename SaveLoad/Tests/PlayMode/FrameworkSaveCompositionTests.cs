using System.Collections;
using System.IO;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;
using VContainer.Unity;

namespace FakeMG.SaveLoad.Tests.PlayMode
{
    public sealed class FrameworkSaveCompositionTests
    {
        private const string FRAMEWORK_PREFAB_GUID = "a66cf0cfc2964994198eea0bdfa71227";
        private const string SETTINGS_FILE_NAME = "settings.json";
        private const string WORLD_ROOT_DIRECTORY_NAME = "Saves";
        private const string BACKUP_DIRECTORY_NAME = "SaveLoadPlayModeTestBackup";
        private const int STARTUP_FRAME_LIMIT = 300;

        private GameObject _saveLoadInstance;
        private SaveLoadTestLifetimeScope _lifetimeScope;
        private IObjectResolver _testContainer;
        private string _backupDirectoryPath;
        private bool _hasIsolatedPersistentSaveFiles;

        #region Unity Lifecycle

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            IsolatePersistentSaveFiles();
            string prefabPath = AssetDatabase.GUIDToAssetPath(FRAMEWORK_PREFAB_GUID);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(prefab, $"Missing framework SaveLoad fixture with GUID {FRAMEWORK_PREFAB_GUID}.");
            SaveLoadTestLifetimeScope lifetimeScopePrefab = prefab.GetComponent<SaveLoadTestLifetimeScope>();
            Assert.IsNotNull(lifetimeScopePrefab, "Framework SaveLoad fixture requires its test lifetime scope.");
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterComponentInNewPrefab(lifetimeScopePrefab, Lifetime.Singleton);
            _testContainer = containerBuilder.Build();
            _lifetimeScope = _testContainer.Resolve<SaveLoadTestLifetimeScope>();
            _saveLoadInstance = _lifetimeScope.gameObject;
            Assert.IsNotNull(_lifetimeScope);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            (_testContainer as System.IDisposable)?.Dispose();
            _testContainer = null;
            if (_saveLoadInstance)
            {
                Object.Destroy(_saveLoadInstance);
            }

            yield return null;
            RestorePersistentSaveFiles();
        }

        #endregion

        #region Public Methods

        [UnityTest]
        public IEnumerator FrameworkPrefab_OnStartup_InitializesGlobalAndSingleWorldPersistence()
        {
            IWorldSaveManager worldSaveManager = _lifetimeScope.Container.Resolve<IWorldSaveManager>();
            IGlobalSaveManager globalSaveManager = _lifetimeScope.Container.Resolve<IGlobalSaveManager>();

            for (int frameCount = 0; frameCount < STARTUP_FRAME_LIMIT && !worldSaveManager.HasActiveWorld; frameCount++)
            {
                yield return null;
            }

            Assert.IsTrue(worldSaveManager.HasActiveWorld, "Resume-or-create did not activate a world.");
            Assert.AreEqual(1, worldSaveManager.GetWorlds().Count);
            string worldId = worldSaveManager.ActiveWorldId;
            string worldDirectoryPath = Path.Combine(Application.persistentDataPath, WORLD_ROOT_DIRECTORY_NAME, worldId);
            string manifestFilePath = Path.Combine(worldDirectoryPath, SaveFileCatalog.WORLD_MANIFEST_FILE_NAME);
            Assert.IsTrue(File.Exists(manifestFilePath));
            Assert.AreEqual(0, Directory.GetFiles(worldDirectoryPath, "manual_*.sav").Length);
            Assert.AreEqual(1, Directory.GetFiles(worldDirectoryPath, "autosave_*.sav").Length);
            AssertProtectedFileIsNotReadableJson(manifestFilePath);

            yield return globalSaveManager.SaveAsync("settings").ToCoroutine();
            string settingsFilePath = Path.Combine(Application.persistentDataPath, SETTINGS_FILE_NAME);
            Assert.IsTrue(File.Exists(settingsFilePath));
            Assert.That(File.ReadAllText(settingsFilePath), Does.Contain(SaveFileCatalog.METADATA_KEY));

            WorldLifecycleAutoSaveSubscriber lifecycleAutoSaveSubscriber =
                _saveLoadInstance.GetComponentInChildren<WorldLifecycleAutoSaveSubscriber>(true);
            Assert.IsNotNull(lifecycleAutoSaveSubscriber);
            lifecycleAutoSaveSubscriber.SendMessage(
                "OnApplicationFocus",
                false,
                SendMessageOptions.RequireReceiver);
            for (int frameCount = 0;
                 frameCount < STARTUP_FRAME_LIMIT && Directory.GetFiles(worldDirectoryPath, "autosave_*.sav").Length < 2;
                 frameCount++)
            {
                yield return null;
            }

            string[] autoSaveFilePaths = Directory.GetFiles(worldDirectoryPath, "autosave_*.sav");
            Assert.AreEqual(2, autoSaveFilePaths.Length);
            Assert.That(autoSaveFilePaths[0], Does.StartWith(worldDirectoryPath));
            Assert.AreEqual(0, Directory.GetFiles(worldDirectoryPath, "manual_*.sav").Length);
            AssertProtectedFileIsNotReadableJson(autoSaveFilePaths[0]);
        }

        #endregion

        #region Private Methods

        private void IsolatePersistentSaveFiles()
        {
            _backupDirectoryPath = Path.Combine(Application.persistentDataPath, BACKUP_DIRECTORY_NAME);
            Assert.IsFalse(
                Directory.Exists(_backupDirectoryPath),
                $"Stale test backup exists at {_backupDirectoryPath}. Restore it before running save tests.");
            Directory.CreateDirectory(_backupDirectoryPath);
            _hasIsolatedPersistentSaveFiles = true;
            MoveFileToBackup(SETTINGS_FILE_NAME);
            MoveFileToBackup(SETTINGS_FILE_NAME + ".bak");

            string worldRootPath = Path.Combine(Application.persistentDataPath, WORLD_ROOT_DIRECTORY_NAME);
            if (Directory.Exists(worldRootPath))
            {
                Directory.Move(worldRootPath, Path.Combine(_backupDirectoryPath, WORLD_ROOT_DIRECTORY_NAME));
            }
        }

        private void RestorePersistentSaveFiles()
        {
            if (!_hasIsolatedPersistentSaveFiles)
            {
                return;
            }

            DeleteFileIfPresent(SETTINGS_FILE_NAME);
            DeleteFileIfPresent(SETTINGS_FILE_NAME + ".bak");
            string worldRootPath = Path.Combine(Application.persistentDataPath, WORLD_ROOT_DIRECTORY_NAME);
            if (Directory.Exists(worldRootPath))
            {
                Directory.Delete(worldRootPath, true);
            }

            RestoreFileFromBackup(SETTINGS_FILE_NAME);
            RestoreFileFromBackup(SETTINGS_FILE_NAME + ".bak");
            string backupWorldRootPath = Path.Combine(_backupDirectoryPath, WORLD_ROOT_DIRECTORY_NAME);
            if (Directory.Exists(backupWorldRootPath))
            {
                Directory.Move(backupWorldRootPath, worldRootPath);
            }

            if (Directory.Exists(_backupDirectoryPath))
            {
                Directory.Delete(_backupDirectoryPath, true);
            }

            _hasIsolatedPersistentSaveFiles = false;
        }

        private void MoveFileToBackup(string fileName)
        {
            string sourcePath = Path.Combine(Application.persistentDataPath, fileName);
            if (File.Exists(sourcePath))
            {
                File.Move(sourcePath, Path.Combine(_backupDirectoryPath, fileName));
            }
        }

        private void RestoreFileFromBackup(string fileName)
        {
            string backupPath = Path.Combine(_backupDirectoryPath, fileName);
            if (File.Exists(backupPath))
            {
                File.Move(backupPath, Path.Combine(Application.persistentDataPath, fileName));
            }
        }

        private static void DeleteFileIfPresent(string fileName)
        {
            string filePath = Path.Combine(Application.persistentDataPath, fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        private static void AssertProtectedFileIsNotReadableJson(string filePath)
        {
            string fileText = File.ReadAllText(filePath);
            Assert.That(fileText, Does.Not.Contain(SaveFileCatalog.METADATA_KEY));
            Assert.That(fileText.TrimStart(), Does.Not.StartWith("{"));
        }

        #endregion
    }
}
