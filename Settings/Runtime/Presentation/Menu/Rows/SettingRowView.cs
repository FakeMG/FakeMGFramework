using UnityEngine;

namespace FakeMG.Settings
{
    public sealed class SettingRowView : MonoBehaviour, ISettingRow
    {
        [SerializeField] private SettingRowHighlight _highlight;

        private ISettingUIBinder _binder;

        public string Title => _binder.SettingSO.Label;
        public string Description => _binder.SettingSO.Description;

        #region Unity Lifecycle

        private void Awake()
        {
            _binder = GetComponent<ISettingUIBinder>();
        }

        #endregion

        #region Public Methods

        public void ShowFocused()
        {
            _highlight.ShowFocused();
        }

        public void ShowUnfocused()
        {
            _highlight.ShowUnfocused();
        }

        #endregion
    }
}
