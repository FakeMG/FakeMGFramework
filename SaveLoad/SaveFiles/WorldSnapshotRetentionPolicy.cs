using System;
using System.Collections.Generic;
using System.Linq;

namespace FakeMG.SaveLoad
{
    public sealed class WorldSnapshotRetentionPolicy
    {
        #region Public Methods

        public IReadOnlyList<WorldSnapshotSummary> SelectExpiredAutoSaves(
            IReadOnlyList<WorldSnapshotSummary> snapshots,
            int maximumAutoSaveCount)
        {
            if (maximumAutoSaveCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumAutoSaveCount));
            }

            WorldSnapshotSummary[] autoSaves = snapshots
                .Where(snapshot => snapshot.SaveKind == SaveFileKind.Auto)
                .OrderBy(snapshot => snapshot.TimestampUtc)
                .ToArray();
            int removalCount = autoSaves.Length - maximumAutoSaveCount;
            if (removalCount <= 0)
            {
                return Array.Empty<WorldSnapshotSummary>();
            }

            return autoSaves.Take(removalCount).ToArray();
        }

        #endregion
    }
}
