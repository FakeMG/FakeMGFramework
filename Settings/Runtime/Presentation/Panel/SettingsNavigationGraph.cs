using System.Collections.Generic;

namespace FakeMG.Settings
{
    /// <summary>Where up, down, left and right lead from one selectable.</summary>
    public readonly struct NavigationLinks<TSelectable> where TSelectable : class
    {
        public TSelectable Up { get; }
        public TSelectable Down { get; }
        public TSelectable Left { get; }
        public TSelectable Right { get; }

        public NavigationLinks(TSelectable up, TSelectable down, TSelectable left, TSelectable right)
        {
            Up = up;
            Down = down;
            Left = left;
            Right = right;
        }
    }

    /// <summary>
    /// Navigation for a settings page: rows of selectables under a row of tab buttons. Up from the first row leads to the shown
    /// tab, and down from any tab leads to the first selectable.
    /// </summary>
    public sealed class SettingsNavigationGraph<TSelectable> where TSelectable : class
    {
        private readonly IReadOnlyList<IReadOnlyList<TSelectable>> _rows;
        private readonly IReadOnlyList<TSelectable> _tabButtons;
        private readonly TSelectable _shownTabButton;

        public IReadOnlyList<IReadOnlyList<TSelectable>> Rows => _rows;

        private TSelectable FirstSelectable => _rows.Count > 0 ? _rows[0][0] : null;

        public SettingsNavigationGraph(IReadOnlyList<IReadOnlyList<TSelectable>> rows, IReadOnlyList<TSelectable> tabButtons, int shownTabIndex)
        {
            _rows = rows;
            _tabButtons = tabButtons;
            _shownTabButton = tabButtons[shownTabIndex];
        }

        #region Public Methods

        /// <summary>Groups selectables in page order into rows; one that belongs to no row gets a row of its own.</summary>
        /// <param name="rowOfSelectable">The owning row of each selectable that has one.</param>
        public static List<IReadOnlyList<TSelectable>> GroupIntoRows<TRow>(IEnumerable<TSelectable> selectablesInPageOrder, IReadOnlyDictionary<TSelectable, TRow> rowOfSelectable)
            where TRow : class
        {
            List<IReadOnlyList<TSelectable>> rows = new();
            List<TSelectable> currentRow = null;
            TRow currentRowOwner = null;
            foreach (TSelectable selectable in selectablesInPageOrder)
            {
                rowOfSelectable.TryGetValue(selectable, out TRow rowOwner);
                if (currentRow == null || rowOwner == null || rowOwner != currentRowOwner)
                {
                    currentRow = new List<TSelectable>();
                    rows.Add(currentRow);
                }

                currentRow.Add(selectable);
                currentRowOwner = rowOwner;
            }

            return rows;
        }

        /// <remarks>Left and right stay empty at a row's ends, so a slider or dropdown can still use them to change its value.</remarks>
        public NavigationLinks<TSelectable> GetSelectableLinks(int rowIndex, int column)
        {
            IReadOnlyList<TSelectable> row = _rows[rowIndex];
            return new NavigationLinks<TSelectable>(
                rowIndex > 0 ? SelectableAt(_rows[rowIndex - 1], column) : _shownTabButton,
                rowIndex < _rows.Count - 1 ? SelectableAt(_rows[rowIndex + 1], column) : null,
                column > 0 ? row[column - 1] : null,
                column < row.Count - 1 ? row[column + 1] : null);
        }

        public NavigationLinks<TSelectable> GetTabLinks(int tabIndex)
        {
            return new NavigationLinks<TSelectable>(
                null,
                FirstSelectable,
                tabIndex > 0 ? _tabButtons[tabIndex - 1] : null,
                tabIndex < _tabButtons.Count - 1 ? _tabButtons[tabIndex + 1] : null);
        }

        #endregion

        #region Private Methods

        /// <remarks>A shorter row leads to its last selectable, so moving down from the right column never dead-ends.</remarks>
        private static TSelectable SelectableAt(IReadOnlyList<TSelectable> row, int column)
        {
            return row[column < row.Count ? column : row.Count - 1];
        }

        #endregion
    }
}
