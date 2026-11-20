using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace FakeMG.Settings
{
    public sealed class SettingRowView : MonoBehaviour, ISettingRow
    {
        [Required, SerializeField] private SettingUIBinder _binder;
        [Required, SerializeField] private SettingRowHighlight _highlight;
        [Required, SerializeField] private Selectable[] _selectables;

        public string Title => _binder.SettingSO.Label;
        public string Description => _binder.SettingSO.Description;
        public IReadOnlyList<Selectable> Selectables => _selectables;

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
