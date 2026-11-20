using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using VContainer;

namespace FakeMG.Settings
{
    /// <summary>Connects one setting to the selectable of its row: shows the stored value and stores the player's changes.</summary>
    public abstract class SettingUIBinder : MonoBehaviour
    {
        [Required, SerializeField] private TMP_Text _labelText;

        protected SettingsStateRepository Settings { get; private set; }

        public abstract SettingDefinitionSO SettingSO { get; }

        #region Unity Lifecycle

        private void Start()
        {
            _labelText.text = SettingSO.Label;
            ShowStoredValue();
        }

        #endregion

        #region Public Methods

        [Inject]
        public void Construct(SettingsStateRepository settings)
        {
            Settings = settings;
        }

        #endregion

        #region Private Methods

        protected abstract void ShowStoredValue();

        #endregion
    }
}
