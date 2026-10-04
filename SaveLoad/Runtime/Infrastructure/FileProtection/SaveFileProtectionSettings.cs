using System;

namespace FakeMG.SaveLoad
{
    public interface ISaveDataStoreProfile
    {
    }

    public sealed class SaveFileProtectionSettings : ISaveDataStoreProfile
    {
        public static SaveFileProtectionSettings Plain { get; } = new(false, false, string.Empty);

        public bool IsEncryptionEnabled { get; }
        public bool IsCompressionEnabled { get; }
        internal string EncryptionPassword { get; }

        public SaveFileProtectionSettings(
            bool isEncryptionEnabled,
            bool isCompressionEnabled,
            string encryptionPassword)
        {
            if (isEncryptionEnabled && string.IsNullOrWhiteSpace(encryptionPassword))
            {
                throw new ArgumentException(
                    "An encryption password is required when save-file encryption is enabled.",
                    nameof(encryptionPassword));
            }

            IsEncryptionEnabled = isEncryptionEnabled;
            IsCompressionEnabled = isCompressionEnabled;
            EncryptionPassword = isEncryptionEnabled ? encryptionPassword : string.Empty;
        }
    }
}
