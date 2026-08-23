using System.Collections.Generic;

namespace FakeMG.SaveLoad
{
    public enum SaveCatalogRejectionReason
    {
        MissingMetadata,
        InvalidMetadata,
        InvalidOwnership,
        InvalidPath,
        IncompatibleProfile,
        CorruptManifest,
        UnsupportedVersion,
    }

    public readonly struct SaveCatalogDiagnostic
    {
        public string FilePath { get; }
        public SaveCatalogRejectionReason Reason { get; }
        public string Message { get; }

        public SaveCatalogDiagnostic(
            string filePath,
            SaveCatalogRejectionReason reason,
            string message)
        {
            FilePath = filePath ?? string.Empty;
            Reason = reason;
            Message = message ?? string.Empty;
        }
    }

    public sealed class SaveCatalogDiscoveryResult
    {
        public IReadOnlyList<ValidatedSaveFileInfo> Files { get; }
        public IReadOnlyList<SaveCatalogDiagnostic> Diagnostics { get; }

        public SaveCatalogDiscoveryResult(
            IReadOnlyList<ValidatedSaveFileInfo> files,
            IReadOnlyList<SaveCatalogDiagnostic> diagnostics)
        {
            Files = files;
            Diagnostics = diagnostics;
        }
    }

    public sealed class WorldCatalogDiscoveryResult
    {
        public IReadOnlyList<WorldSummary> Worlds { get; }
        public IReadOnlyList<SaveCatalogDiagnostic> Diagnostics { get; }

        public WorldCatalogDiscoveryResult(
            IReadOnlyList<WorldSummary> worlds,
            IReadOnlyList<SaveCatalogDiagnostic> diagnostics)
        {
            Worlds = worlds;
            Diagnostics = diagnostics;
        }
    }
}
