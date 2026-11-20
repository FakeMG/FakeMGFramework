using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework.UI.Tab;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace FakeMG.Settings
{
    /// <summary>Links keyboard and gamepad navigation between the tabs and the rows of the shown page.</summary>
    /// <remarks>
    /// Links navigation explicitly because automatic navigation searches every enabled selectable in every scene, so it could
    /// jump into another canvas: it once reached a copy of this panel left loaded in an editor preview scene.
    /// </remarks>
    public sealed class SettingsTabNavigation : MonoBehaviour
    {
        [Required, SerializeField] private TabSystem _tabs;

        private readonly Dictionary<Selectable, ISettingRow> _rowOfSelectable = new();
        private readonly List<Selectable> _tabButtons = new();

        #region Public Methods

        /// <remarks>A page may show other rows for another device, e.g. a key binding list hides keyboard-only actions.</remarks>
        public void LinkCurrentPage()
        {
            if (_tabs.IsValidTabIndex(_tabs.CurrentTabIndex))
            {
                LinkShownPage(_tabs.CurrentTabIndex);
            }
        }

        public void LinkShownPage(int shownTabIndex)
        {
            LinkAfterPageBuildsAsync(shownTabIndex, destroyCancellationToken).Forget();
        }

        /// <returns>Null when the object is not a selectable of a row on the shown page.</returns>
        public ISettingRow FindRowOf(GameObject selectedObject)
        {
            return selectedObject && selectedObject.TryGetComponent(out Selectable selectable) && _rowOfSelectable.TryGetValue(selectable, out ISettingRow row)
                ? row
                : null;
        }

        #endregion

        #region Private Methods

        /// <remarks>Waits a frame: the shown page may still be enabling or hiding its rows when the tab changes.</remarks>
        private async UniTaskVoid LinkAfterPageBuildsAsync(int shownTabIndex, CancellationToken cancellationToken)
        {
            await UniTask.NextFrame(cancellationToken);
            if (_tabs.CurrentTabIndex != shownTabIndex)
            {
                return;
            }

            RectTransform page = _tabs.GetTabContent(shownTabIndex);
            MapSelectablesToRows(page);
            CollectTabButtons();
            var graph = new SettingsNavigationGraph<Selectable>(
                SettingsNavigationGraph<Selectable>.GroupIntoRows(GetNavigableSelectables(page), _rowOfSelectable),
                _tabButtons,
                shownTabIndex);
            ApplyLinks(graph);
        }

        private void MapSelectablesToRows(RectTransform page)
        {
            _rowOfSelectable.Clear();
            foreach (ISettingRow row in page.GetComponentsInChildren<ISettingRow>())
            {
                foreach (Selectable selectable in row.Selectables)
                {
                    _rowOfSelectable[selectable] = row;
                }
            }
        }

        private void CollectTabButtons()
        {
            _tabButtons.Clear();
            for (int tabIndex = 0; tabIndex < _tabs.GetTabCount(); tabIndex++)
            {
                _tabButtons.Add(_tabs.GetTabButton(tabIndex).Button);
            }
        }

        private static IEnumerable<Selectable> GetNavigableSelectables(RectTransform page)
        {
            foreach (Selectable selectable in page.GetComponentsInChildren<Selectable>())
            {
                if (selectable.IsInteractable() && selectable.navigation.mode != Navigation.Mode.None)
                {
                    yield return selectable;
                }
            }
        }

        private void ApplyLinks(SettingsNavigationGraph<Selectable> graph)
        {
            for (int rowIndex = 0; rowIndex < graph.Rows.Count; rowIndex++)
            {
                IReadOnlyList<Selectable> row = graph.Rows[rowIndex];
                for (int column = 0; column < row.Count; column++)
                {
                    row[column].navigation = ToNavigation(graph.GetSelectableLinks(rowIndex, column));
                }
            }

            for (int tabIndex = 0; tabIndex < _tabButtons.Count; tabIndex++)
            {
                _tabButtons[tabIndex].navigation = ToNavigation(graph.GetTabLinks(tabIndex));
            }
        }

        private static Navigation ToNavigation(NavigationLinks<Selectable> links)
        {
            return new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = links.Up,
                selectOnDown = links.Down,
                selectOnLeft = links.Left,
                selectOnRight = links.Right
            };
        }

        #endregion
    }
}
