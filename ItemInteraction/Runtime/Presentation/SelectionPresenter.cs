using System.Collections.Generic;

namespace FakeMG.ItemInteraction
{
    /// <summary>
    /// Keeps a character's selection presentations on its selected item: a new selection cancels the running use,
    /// hides the old item, and shows the new one once its held prefab can be presented.
    /// </summary>
    public sealed class SelectionPresenter
    {
        private readonly ISelectedItemSource _selectedItemSource;
        private readonly ISelectedItemInstanceSource _selectedItemInstanceSource;
        private readonly HeldItemPrefabPool _heldItemPrefabPool;
        private readonly IInteractionSelectionLifecycle _interactionLifecycle;
        private readonly IReadOnlyList<IItemSelectionPresentation> _selectionPresentations;

        private IInteractionItem _presentedItem;
        private string _presentedInstanceId;
        private bool _isShown;

        public SelectionPresenter(
            ISelectedItemSource selectedItemSource,
            HeldItemPrefabPool heldItemPrefabPool,
            IInteractionSelectionLifecycle interactionLifecycle,
            IReadOnlyList<IItemSelectionPresentation> selectionPresentations)
        {
            _selectedItemSource = selectedItemSource;
            // Sources that identify item instances let a re-selection of the same item, such as a refilled slot, re-present it.
            _selectedItemInstanceSource = selectedItemSource as ISelectedItemInstanceSource;
            _heldItemPrefabPool = heldItemPrefabPool;
            _interactionLifecycle = interactionLifecycle;
            _selectionPresentations = selectionPresentations;
        }

        #region Public Methods

        public void PresentSelectedItem()
        {
            IInteractionItem selectedItem = _selectedItemSource.SelectedItem;
            string instanceId = _selectedItemInstanceSource?.SelectedItemInstanceId;
            if (selectedItem == _presentedItem && !string.IsNullOrEmpty(instanceId) && _presentedInstanceId == instanceId)
            {
                return;
            }

            _presentedItem = selectedItem;
            _presentedInstanceId = instanceId;
            _interactionLifecycle.ChangeSelection();
            HideSelection();
            ShowSelectionWhenReady();
        }

        public void ShowSelectionWhenReady()
        {
            if (_isShown || !_heldItemPrefabPool.CanPresent(_presentedItem))
            {
                return;
            }

            _isShown = true;
            foreach (IItemSelectionPresentation selectionPresentation in _selectionPresentations)
            {
                selectionPresentation.ShowSelection(_presentedItem);
            }
        }

        public void CancelAndClear()
        {
            _interactionLifecycle.Cancel();
            HideSelection();
            _presentedItem = null;
            _presentedInstanceId = null;
        }

        #endregion

        #region Private Methods

        private void HideSelection()
        {
            if (!_isShown)
            {
                return;
            }

            _isShown = false;
            foreach (IItemSelectionPresentation selectionPresentation in _selectionPresentations)
            {
                selectionPresentation.HideSelection();
            }
        }

        #endregion
    }
}
