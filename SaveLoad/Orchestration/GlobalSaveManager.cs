using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;

namespace FakeMG.SaveLoad
{
    public sealed class GlobalSaveManager : IGlobalSaveManager, IDisposable
    {
        private static readonly IAsyncSaveParticipant[] NoParticipants = Array.Empty<IAsyncSaveParticipant>();

        private readonly SaveFileService _saveFileService;
        private readonly ISaveTimeProvider _saveTimeProvider;
        private readonly IReadOnlyDictionary<string, RegisteredGlobalDocument> _documentsById;

        public GlobalSaveManager(
            SaveFileService saveFileService,
            ISaveTimeProvider saveTimeProvider,
            IEnumerable<IGlobalSaveDocument> documents)
        {
            _saveFileService = saveFileService;
            _saveTimeProvider = saveTimeProvider;
            _documentsById = RegisterDocuments(documents);
        }

        #region Public Methods

        public async UniTask<GlobalSaveInitializationResult> InitializeAsync(CancellationToken cancellationToken = default)
        {
            List<string> failureReasons = new();
            foreach (RegisteredGlobalDocument registration in _documentsById.Values)
            {
                SaveFileLoadResult result = await _saveFileService.LoadAsync(
                    registration.Descriptor,
                    registration.Saveables,
                    NoParticipants,
                    true,
                    true,
                    cancellationToken);
                if (!result.Succeeded)
                {
                    failureReasons.Add($"Global document '{registration.Document.DocumentId}': {result.FailureReason}");
                }
            }

            return new GlobalSaveInitializationResult(failureReasons.Count == 0, failureReasons);
        }

        public async UniTask<GlobalDocumentSaveResult> SaveAsync(string documentId, CancellationToken cancellationToken = default)
        {
            if (!_documentsById.TryGetValue(documentId, out RegisteredGlobalDocument registration))
            {
                string failureReason = $"Cannot save unregistered global document '{documentId}'.";
                Echo.Error(failureReason);
                return new GlobalDocumentSaveResult(
                    documentId,
                    new SaveFileWriteResult(SaveFileWriteStatus.Failed, string.Empty, failureReason));
            }

            await registration.SaveLock.WaitAsync(cancellationToken);
            try
            {
                IReadOnlyDictionary<string, object> capturedStates;
                try
                {
                    capturedStates = SaveableCollection.Capture(registration.Saveables);
                }
                catch (Exception exception)
                {
                    string failureReason = $"Failed to capture global document '{documentId}': {exception}";
                    Echo.Error(failureReason);
                    return new GlobalDocumentSaveResult(
                        documentId,
                        new SaveFileWriteResult(SaveFileWriteStatus.Failed, registration.Descriptor.FilePath, failureReason));
                }

                SaveFileWriteResult fileResult = await _saveFileService.SaveAsync(
                    registration.Descriptor,
                    capturedStates,
                    NoParticipants,
                    _saveTimeProvider.GetUtcNow(),
                    cancellationToken);
                return new GlobalDocumentSaveResult(documentId, fileResult);
            }
            finally
            {
                registration.SaveLock.Release();
            }
        }

        public void Dispose()
        {
            foreach (RegisteredGlobalDocument registration in _documentsById.Values)
            {
                registration.Dispose();
            }
        }

        #endregion

        #region Private Methods

        private static IReadOnlyDictionary<string, RegisteredGlobalDocument> RegisterDocuments(IEnumerable<IGlobalSaveDocument> documents)
        {
            List<GlobalDocumentCandidate> candidates = new();
            List<string> failureReasons = new();
            foreach (IGlobalSaveDocument document in documents)
            {
                try
                {
                    if (document == null || string.IsNullOrWhiteSpace(document.DocumentId))
                    {
                        failureReasons.Add("Global document ID is required.");
                        continue;
                    }

                    string filePath = SaveFileCatalog.CreateGlobalSaveFilePath(document.FileName);
                    var descriptor = new SaveFileDescriptor(
                        filePath,
                        document.DocumentId,
                        SaveFileKind.GlobalDocument,
                        document.StorageProfile);
                    candidates.Add(new GlobalDocumentCandidate(document, descriptor));
                }
                catch (Exception exception)
                {
                    failureReasons.Add(
                        $"Global document '{document?.DocumentId ?? "missing"}' is invalid: {exception}");
                }
            }

            foreach (IGrouping<string, GlobalDocumentCandidate> duplicateIdGroup in candidates
                         .GroupBy(candidate => candidate.Document.DocumentId, StringComparer.Ordinal)
                         .Where(group => group.Count() > 1))
            {
                failureReasons.Add($"Duplicate global document ID '{duplicateIdGroup.Key}' is used by {duplicateIdGroup.Count()} registrations.");
            }

            foreach (IGrouping<string, GlobalDocumentCandidate> duplicateFileGroup in candidates
                         .GroupBy(candidate => candidate.Descriptor.FilePath, StringComparer.OrdinalIgnoreCase)
                         .Where(group => group.Count() > 1))
            {
                failureReasons.Add($"Duplicate global save file '{duplicateFileGroup.Key}' is used by {duplicateFileGroup.Count()} registrations.");
            }

            if (failureReasons.Count > 0)
            {
                string failureReason = string.Join(Environment.NewLine, failureReasons);
                Echo.Error(failureReason);
                throw new InvalidOperationException(failureReason);
            }

            Dictionary<string, RegisteredGlobalDocument> registrations = new(StringComparer.Ordinal);
            foreach (GlobalDocumentCandidate candidate in candidates)
            {
                registrations.Add(candidate.Document.DocumentId, new RegisteredGlobalDocument(candidate.Document, candidate.Descriptor));
            }

            return registrations;
        }

        private readonly struct GlobalDocumentCandidate
        {
            public IGlobalSaveDocument Document { get; }
            public SaveFileDescriptor Descriptor { get; }

            public GlobalDocumentCandidate(IGlobalSaveDocument document, SaveFileDescriptor descriptor)
            {
                Document = document;
                Descriptor = descriptor;
            }
        }

        private sealed class RegisteredGlobalDocument : IDisposable
        {
            public IGlobalSaveDocument Document { get; }
            public SaveFileDescriptor Descriptor { get; }
            public IReadOnlyDictionary<string, ISaveable> Saveables { get; }
            public SemaphoreSlim SaveLock { get; } = new(1, 1);

            public RegisteredGlobalDocument(IGlobalSaveDocument document, SaveFileDescriptor descriptor)
            {
                Document = document;
                Descriptor = descriptor;
                Saveables = SaveableRegistration.Create(new ISaveable[] { document });
            }

            public void Dispose()
            {
                SaveLock.Dispose();
            }
        }

        #endregion
    }
}
