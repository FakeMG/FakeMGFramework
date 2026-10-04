using FakeMG.Framework;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FakeMG.SaveLoad
{
    [CreateAssetMenu(fileName = "Save File Protection", menuName = FakeMGEditorMenus.ROOT + "/Save Load/Save File Protection")]
    public sealed class SaveFileProtectionSO : ScriptableObject
    {
        [Header("File Protection")]
        [Tooltip("Encrypts save bytes using Easy Save AES.")]
        [SerializeField] private bool _isEncryptionEnabled;
        [Tooltip("Compresses save bytes using Gzip.")]
        [SerializeField] private bool _isCompressionEnabled;
        [Space]
        [Tooltip("Password used only when encryption is enabled. Changing it invalidates existing encrypted files.")]
        [ShowIf(nameof(_isEncryptionEnabled))]
        [ValidateInput(nameof(IsEncryptionPasswordValid), "Encrypted save profiles require a password.")]
        [SerializeField] private string _encryptionPassword;

        #region Public Methods

        public SaveFileProtectionSettings CreateSettings()
        {
            return new SaveFileProtectionSettings(_isEncryptionEnabled, _isCompressionEnabled, _encryptionPassword);
        }

        #endregion

        #region Private Methods

        private bool IsEncryptionPasswordValid(string encryptionPassword)
        {
            return !_isEncryptionEnabled || !string.IsNullOrWhiteSpace(encryptionPassword);
        }

        #endregion
    }
}
