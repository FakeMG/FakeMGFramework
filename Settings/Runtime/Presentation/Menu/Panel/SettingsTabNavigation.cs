using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework.UI.Tab;
using FakeMG.InputHints;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace FakeMG.Settings
{
    /// <remarks>
    /// Links navigation explicitly because automatic navigation searches every enabled control in every scene, so it could
    /// jump into another canvas: it once reached a copy of this panel left loaded in an editor preview scene.
    /// </remarks>
    public sealed class SettingsTabNavigation : MonoBehaviour
    {
        [SerializeField] private TabSystem _tabs;

        private readonly List<List<Selectable>> _rows = new();
        private ActiveGlyphSetTracker _glyphSetTracker;

        #region Unity Lifecycle

        private void OnEnable()
        {
            _tabs.OnTabChanged += LinkShownPage;
            _glyphSetTracker.OnGlyphsChanged += LinkCurrentPage;
        }

        private void OnDisable()
        {
            _tabs.OnTabChanged -= LinkShownPage;
            _glyphSetTracker.OnGlyphsChanged -= LinkCurrentPage;
        }

        #endregion

        #region Public Methods

        [Inject]
        public void Construct(ActiveGlyphSetTracker glyphSetTracker)
        {
            _glyphSetTracker = glyphSetTracker;
        }

        #endregion

        #region Private Methods

        /// <remarks>A page may show other rows for another device, e.g. a key binding list hides keyboard-only actions.</remarks>
        private void LinkCurrentPage()
        {
            if (_tabs.IsValidTabIndex(_tabs.CurrentTabIndex))
            {
                LinkShownPage(_tabs.CurrentTabIndex);
            }
        }

        private void LinkShownPage(int shownTabIndex)
        {
            LinkAfterPageBuildsAsync(shownTabIndex, destroyCancellationToken).Forget();
        }

        /// <remarks>Waits a frame: the shown page may still be enabling or hiding its rows when the tab changes.</remarks>
        private async UniTaskVoid LinkAfterPageBuildsAsync(int shownTabIndex, CancellationToken cancellationToken)
        {
            await UniTask.NextFrame(cancellationToken);
            if (_tabs.CurrentTabIndex != shownTabIndex)
            {
                return;
            }

            CollectRows(_tabs.GetTabContent(shownTabIndex));
            Selectable shownTabButton = _tabs.GetTabButton(shownTabIndex).Button;
            LinkRows(shownTabButton);
            LinkTabs(_rows.Count > 0 ? _rows[0][0] : null);
        }

        private void CollectRows(RectTransform page)
        {
            _rows.Clear();
            Component currentRowOwner = null;
            foreach (Selectable control in page.GetComponentsInChildren<Selectable>())
            {
                if (!control.IsInteractable() || control.navigation.mode == Navigation.Mode.None)
                {
                    continue;
                }

                Component rowOwner = control.GetComponentInParent<ISettingRow>() as Component;
                if (!rowOwner || rowOwner != currentRowOwner)
                {
                    _rows.Add(new List<Selectable>());
                }

                _rows[^1].Add(control);
                currentRowOwner = rowOwner;
            }
        }

        /// <remarks>Left and right stay empty at a row's ends, so a slider or dropdown can still use them to change its value.</remarks>
        private void LinkRows(Selectable shownTabButton)
        {
            for (int rowIndex = 0; rowIndex < _rows.Count; rowIndex++)
            {
                List<Selectable> row = _rows[rowIndex];
                for (int column = 0; column < row.Count; column++)
                {
                    row[column].navigation = new Navigation
                    {
                        mode = Navigation.Mode.Explicit,
                        selectOnUp = rowIndex > 0 ? ControlAt(_rows[rowIndex - 1], column) : shownTabButton,
                        selectOnDown = rowIndex < _rows.Count - 1 ? ControlAt(_rows[rowIndex + 1], column) : null,
                        selectOnLeft = column > 0 ? row[column - 1] : null,
                        selectOnRight = column < row.Count - 1 ? row[column + 1] : null
                    };
                }
            }
        }

        private static Selectable ControlAt(List<Selectable> row, int column)
        {
            return row[Mathf.Min(column, row.Count - 1)];
        }

        private void LinkTabs(Selectable firstControl)
        {
            int tabCount = _tabs.GetTabCount();
            for (int tabIndex = 0; tabIndex < tabCount; tabIndex++)
            {
                Button tabButton = _tabs.GetTabButton(tabIndex).Button;
                tabButton.navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = tabIndex > 0 ? _tabs.GetTabButton(tabIndex - 1).Button : null,
                    selectOnRight = tabIndex < tabCount - 1 ? _tabs.GetTabButton(tabIndex + 1).Button : null,
                    selectOnDown = firstControl
                };
            }
        }

        #endregion
    }
}
