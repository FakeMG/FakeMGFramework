using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace FakeMG.SaveLoad
{
    /// <summary>
    /// Provides the Unity-facing save/load façade and lifecycle. Focused collaborators own state
    /// discovery, request coalescing, save transactions, loading, recovery, and concrete storage.
    /// </summary>
    public class SaveLoadSystem : MonoBehaviour, ISaveRequester, IAsyncSaveParticipantRegistry
    {
        private const float MINIMUM_AUTO_SAVE_INTERVAL_SECONDS = 30f;

        [Header("Storage")]
        [Tooltip("Relative directory path for this save system. Leave empty to save in the root directory.")]
        [SerializeField] private string _saveDirectoryPath = string.Empty;
        [SerializeField] private SaveFileMode _saveFileMode = SaveFileMode.TimestampedFiles;
        [ShowIf(nameof(UsesFixedSaveFileMode))]
        [SerializeField] private string _fixedSaveFileName = SaveFileCatalog.DEFAULT_FIXED_SAVE_FILE_NAME;
        [Tooltip("Optional root used when collecting Saveable components. Leave empty to scan this object hierarchy.")]
        [SerializeField] private Transform _saveablesRoot;
        [Tooltip("Disable this when a runtime world catalog selects the fixed save directory.")]
        [SerializeField] private bool _initializeOnAwake = true;
        [SerializeField] private bool _enableAutoSave = true;
        [SerializeField] private int _maxAutoSaves = 5;
        [SerializeField] private float _autoSaveIntervalSeconds = 300f;
        [SerializeField] private bool _enableDebug = true;

        [Header("Migration")]
        [SerializeField] private MigrationRegistrySO _migrationRegistrySO;

        private CancellationTokenSource _lifetimeCancellationSource;
        private ISaveDataStore _saveDataStore;
        private SaveFileCatalog _saveFileCatalog;
        private SaveStateRegistry _stateRegistry;
        private SaveOperationExecutor _saveOperationExecutor;
        private LoadOperationExecutor _loadOperationExecutor;
        private SaveRequestCoordinator _saveRequestCoordinator;
        private AutoSaveSchedule _autoSaveSchedule;
        private string _normalizedSaveDirectoryPath;
        private string _fixedSaveFilePath;
        private bool _hasInitialized;

        public bool HasInitialized => _hasInitialized;
        public bool IsSaving => _saveRequestCoordinator?.IsSaving == true;
        public string SaveDirectoryPath => _normalizedSaveDirectoryPath;
        public string FixedSaveFilePath => _fixedSaveFilePath;

        public event Action OnLoadingComplete;

        #region Unity Lifecycle

        private void Awake()
        {
            _lifetimeCancellationSource ??= new CancellationTokenSource();
            _autoSaveSchedule ??= new AutoSaveSchedule(_autoSaveIntervalSeconds);
        }

        private void Start()
        {
            if (_initializeOnAwake)
            {
                InitializeConfiguredStorageAsync(_lifetimeCancellationSource.Token).Forget();
            }
        }

        private void Update()
        {
            if (!_hasInitialized || !_enableAutoSave || !_autoSaveSchedule.Advance(Time.deltaTime))
            {
                return;
            }

            TriggerAutoSaveAsync(_lifetimeCancellationSource.Token).Forget();
        }

        private void OnDestroy()
        {
            _lifetimeCancellationSource?.Cancel();
            _lifetimeCancellationSource?.Dispose();
            _lifetimeCancellationSource = null;
        }

        #endregion

        #region Public Methods

        [Inject]
        public void Construct(ISaveDataStore saveDataStore, IAtomicFileTransaction atomicFileTransaction)
        {
            _lifetimeCancellationSource ??= new CancellationTokenSource();
            _autoSaveSchedule ??= new AutoSaveSchedule(_autoSaveIntervalSeconds);
            _saveDataStore = saveDataStore;
            _saveFileCatalog = new SaveFileCatalog(saveDataStore);
            _stateRegistry = new SaveStateRegistry();
            _saveOperationExecutor = new SaveOperationExecutor(saveDataStore, atomicFileTransaction, _saveFileCatalog, _stateRegistry);
            VersionMigrator versionMigrator = _migrationRegistrySO != null
                ? new VersionMigrator(_migrationRegistrySO, saveDataStore)
                : null;
            _loadOperationExecutor = new LoadOperationExecutor(saveDataStore, atomicFileTransaction, _stateRegistry, versionMigrator, NotifyLoadingComplete);
            _saveRequestCoordinator = new SaveRequestCoordinator(ExecuteSaveAsync, _lifetimeCancellationSource.Token);
        }

        public void RefreshSaveables()
        {
            Transform collectionRoot = _saveablesRoot != null ? _saveablesRoot : transform;
            _stateRegistry.Refresh(collectionRoot);
            Echo.Log(
                $"Registered {_stateRegistry.Saveables.Count} Saveable components and " +
                $"{_stateRegistry.AsyncParticipants.Count} asynchronous participants.",
                _enableDebug,
                this);
        }

        public bool RegisterAsyncSaveParticipant(IAsyncSaveParticipant participant)
        {
            return _stateRegistry.RegisterAsyncParticipant(participant);
        }

        public void UnregisterAsyncSaveParticipant(IAsyncSaveParticipant participant)
        {
            _stateRegistry.UnregisterAsyncParticipant(participant);
        }

        public async UniTask<bool> OpenFixedSaveAsync(
            string saveDirectoryPath,
            string fixedSaveFileName,
            CancellationToken cancellationToken = default)
        {
            if (IsSaving)
            {
                Echo.Error("Cannot open a fixed save while a save operation is active.", _enableDebug, this);
                return false;
            }

            _saveDirectoryPath = saveDirectoryPath;
            _fixedSaveFileName = fixedSaveFileName;
            _saveFileMode = SaveFileMode.FixedFile;
            _hasInitialized = false;
            return await InitializeConfiguredStorageAsync(cancellationToken);
        }

        public UniTask<bool> SaveGameAsync(CancellationToken cancellationToken = default)
        {
            SaveFileKind saveKind = UsesFixedSaveFileMode() ? SaveFileKind.Fixed : SaveFileKind.Manual;
            return RequestSaveAsync(saveKind, cancellationToken);
        }

        public async UniTask<bool> LoadGameAsync(string saveFilePath, CancellationToken cancellationToken = default)
        {
            if (!_hasInitialized)
            {
                Echo.Error("Cannot load before the save system has initialized.", _enableDebug, this);
                return false;
            }

            if (IsSaving)
            {
                await _saveRequestCoordinator.WaitForActiveSaveAsync(cancellationToken);
            }

            string normalizedSaveFilePath = SaveFileCatalog.NormalizeSaveFilePath(saveFilePath, _normalizedSaveDirectoryPath);
            return await _loadOperationExecutor.LoadAsync(_normalizedSaveDirectoryPath, normalizedSaveFilePath, cancellationToken);
        }

        public bool DeleteSave(string saveFilePath)
        {
            string normalizedSaveFilePath = SaveFileCatalog.NormalizeSaveFilePath(saveFilePath, _normalizedSaveDirectoryPath);
            if (!_saveDataStore.FileExists(normalizedSaveFilePath))
            {
                Echo.Warning($"Cannot delete missing save {normalizedSaveFilePath}.", _enableDebug, this);
                return false;
            }

            _saveDataStore.DeleteFile(normalizedSaveFilePath);
            Echo.Log($"{normalizedSaveFilePath} deleted.", _enableDebug, this);
            return true;
        }

        public UniTask<bool> TriggerAutoSaveAsync(CancellationToken cancellationToken = default)
        {
            if (!_enableAutoSave)
            {
                Echo.Warning("Automatic save was requested while automatic saving is disabled.", _enableDebug, this);
                return UniTask.FromResult(false);
            }

            return RequestSaveAsync(SaveFileKind.Auto, cancellationToken);
        }

        public void SetAutoSaveInterval(float intervalSeconds)
        {
            _autoSaveIntervalSeconds = Mathf.Max(MINIMUM_AUTO_SAVE_INTERVAL_SECONDS, intervalSeconds);
            _autoSaveSchedule.Reset(_autoSaveIntervalSeconds);
        }

        public void SetAutoSaveEnabled(bool isAutoSaveEnabled)
        {
            _enableAutoSave = isAutoSaveEnabled;
            if (isAutoSaveEnabled)
            {
                _autoSaveSchedule.Reset(_autoSaveIntervalSeconds);
            }
        }

        #endregion

        #region Private Methods

        private async UniTask<bool> InitializeConfiguredStorageAsync(CancellationToken cancellationToken)
        {
            if (!TryInitializeStorageConfiguration())
            {
                enabled = false;
                return false;
            }

            _autoSaveSchedule.Reset(_autoSaveIntervalSeconds);
            RefreshSaveables();
            _hasInitialized = true;
            return await LoadMostRecentSaveAsync(cancellationToken);
        }

        private UniTask<bool> RequestSaveAsync(SaveFileKind requestedSaveKind, CancellationToken cancellationToken)
        {
            if (!_hasInitialized)
            {
                Echo.Error("Cannot save before the save system has initialized.", _enableDebug, this);
                return UniTask.FromResult(false);
            }

            return _saveRequestCoordinator.RequestAsync(requestedSaveKind, cancellationToken);
        }

        private UniTask<bool> ExecuteSaveAsync(SaveFileKind saveKind, CancellationToken cancellationToken)
        {
            return _saveOperationExecutor.ExecuteAsync(
                saveKind,
                _normalizedSaveDirectoryPath,
                _fixedSaveFilePath,
                UsesFixedSaveFileMode(),
                _maxAutoSaves,
                cancellationToken);
        }

        private async UniTask<bool> LoadMostRecentSaveAsync(CancellationToken cancellationToken)
        {
            if (UsesFixedSaveFileMode())
            {
                return await _loadOperationExecutor.LoadAsync(_normalizedSaveDirectoryPath, _fixedSaveFilePath, cancellationToken);
            }

            ManagedSaveFileInfo mostRecentSave = _saveFileCatalog
                .GetManagedSaveFiles(_normalizedSaveDirectoryPath)
                .OrderByDescending(saveFile => saveFile.Metadata.GetTimestampUtc())
                .FirstOrDefault();
            if (mostRecentSave != null)
            {
                return await LoadGameAsync(mostRecentSave.SaveFilePath, cancellationToken);
            }

            await _loadOperationExecutor.LoadDefaultAsync(_normalizedSaveDirectoryPath, string.Empty, cancellationToken);
            return true;
        }

        private bool TryInitializeStorageConfiguration()
        {
            try
            {
                _normalizedSaveDirectoryPath = SaveFileCatalog.NormalizeSaveDirectoryPath(_saveDirectoryPath);
                _fixedSaveFilePath = UsesFixedSaveFileMode()
                    ? SaveFileCatalog.CreateFixedSaveFilePath(_normalizedSaveDirectoryPath, _fixedSaveFileName)
                    : null;
                return true;
            }
            catch (ArgumentException exception)
            {
                Echo.Error($"Save storage configuration is invalid on {name}: {exception.Message}", _enableDebug, this);
                return false;
            }
        }

        private bool UsesFixedSaveFileMode()
        {
            return _saveFileMode == SaveFileMode.FixedFile;
        }

        private void NotifyLoadingComplete()
        {
            OnLoadingComplete?.Invoke();
        }

        #endregion
    }
}
