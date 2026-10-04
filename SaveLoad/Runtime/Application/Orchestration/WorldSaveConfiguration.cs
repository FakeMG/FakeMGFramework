using System;

namespace FakeMG.SaveLoad
{
    public sealed class WorldSaveConfiguration
    {
        public const int DEFAULT_MAXIMUM_AUTO_SAVE_COUNT = 5;
        public const float DEFAULT_AUTO_SAVE_INTERVAL_SECONDS = 300f;
        public const float DEFAULT_FLUSH_TIMEOUT_SECONDS = 10f;
        public const float MINIMUM_AUTO_SAVE_INTERVAL_SECONDS = 30f;

        public int MaximumAutoSaveCount { get; }
        public float AutoSaveIntervalSeconds { get; }
        public float FlushTimeoutSeconds { get; }
        public bool IsAutoSaveEnabled { get; }
        public string DefaultWorldDisplayName { get; }
        public WorldStartupPolicySO StartupPolicySO { get; }

        public WorldSaveConfiguration(
            int maximumAutoSaveCount,
            float autoSaveIntervalSeconds,
            float flushTimeoutSeconds,
            bool isAutoSaveEnabled,
            string defaultWorldDisplayName,
            WorldStartupPolicySO startupPolicySO)
        {
            if (maximumAutoSaveCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumAutoSaveCount));
            }

            if (autoSaveIntervalSeconds < MINIMUM_AUTO_SAVE_INTERVAL_SECONDS)
            {
                throw new ArgumentOutOfRangeException(nameof(autoSaveIntervalSeconds));
            }

            if (flushTimeoutSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(flushTimeoutSeconds));
            }

            MaximumAutoSaveCount = maximumAutoSaveCount;
            AutoSaveIntervalSeconds = autoSaveIntervalSeconds;
            FlushTimeoutSeconds = flushTimeoutSeconds;
            IsAutoSaveEnabled = isAutoSaveEnabled;
            DefaultWorldDisplayName = string.IsNullOrWhiteSpace(defaultWorldDisplayName)
                ? throw new ArgumentException("Default world display name is required.", nameof(defaultWorldDisplayName))
                : defaultWorldDisplayName.Trim();
            StartupPolicySO = startupPolicySO
                ? startupPolicySO
                : throw new ArgumentNullException(nameof(startupPolicySO));
        }
    }
}
