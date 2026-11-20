using System.Collections.Generic;
using UnityEngine.UI;

namespace FakeMG.Settings
{
    public interface ISettingRow
    {
        string Title { get; }
        string Description { get; }

        /// <summary>The row's selectables, left to right. Inactive or non-interactable ones are skipped by navigation.</summary>
        IReadOnlyList<Selectable> Selectables { get; }

        void ShowFocused();
        void ShowUnfocused();
    }
}
