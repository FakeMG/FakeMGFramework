using System;

namespace FakeMG.ItemInteraction
{
    /// <summary>
    /// The item a character has selected. It is never null: with nothing held, the owner's empty-hand item is selected.
    /// </summary>
    public sealed class ItemSelectionState : ISelectionChangeSource
    {
        private readonly IActionItem _emptyHandItem;

        public IInteractionItem SelectedItem { get; private set; }
        public event Action OnSelectedItemChanged;

        public ItemSelectionState(IActionItem emptyHandItem)
        {
            _emptyHandItem = emptyHandItem;
            SelectedItem = emptyHandItem;
        }

        #region Public Methods

        public void Select(IInteractionItem item)
        {
            if (SelectedItem == item)
            {
                return;
            }

            SelectedItem = item;
            OnSelectedItemChanged?.Invoke();
        }

        public void SelectEmptyHand()
        {
            Select(_emptyHandItem);
        }

        #endregion
    }
}
