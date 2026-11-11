using System;

namespace FakeMG.ItemInteraction
{
    public sealed class ItemSelectionState : ISelectionChangeSource
    {
        public IInteractionItem ActiveItem { get; private set; }
        public event Action OnActiveItemChanged;

        #region Public Methods

        public void Select(IInteractionItem item)
        {
            if (ActiveItem == item)
            {
                return;
            }

            ActiveItem = item;
            OnActiveItemChanged?.Invoke();
        }

        #endregion
    }
}
