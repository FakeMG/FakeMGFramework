using UnityEngine;

namespace FakeMG.ItemInteraction
{
    public sealed class SelectionLifecycleSubscriber : MonoBehaviour
    {
        private ISelectionChangeSource _selectionChangeSource;
        private HeldItemPrefabPool _heldItemPrefabPool;
        private InteractionInputState _inputState;
        private IItemSelectionPresentation[] _selectionPresentations;
        private bool _isSubscribed;
        private bool _hasPresentedSelection;
        private IInteractionItem _selectedItem;
        private string _selectedInstanceId;

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (_selectionChangeSource != null)
            {
                Subscribe();
            }
        }

        private void OnDisable()
        {
            UnsubscribeAndClear();
        }

        #endregion

        #region Public Methods

        public void Bind(
            ISelectionChangeSource selectionChangeSource,
            HeldItemPrefabPool heldItemPrefabPool,
            InteractionInputState inputState,
            params IItemSelectionPresentation[] selectionPresentations)
        {
            UnsubscribeAndClear();
            _selectionChangeSource = selectionChangeSource;
            _heldItemPrefabPool = heldItemPrefabPool;
            _inputState = inputState;
            _selectionPresentations = selectionPresentations;

            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        #endregion

        #region Private Methods

        private void Subscribe()
        {
            if (_isSubscribed)
            {
                return;
            }

            _selectionChangeSource.OnActiveItemChanged += RefreshSelection;
            _heldItemPrefabPool.OnPresentationAssetsReady += ApplyReadyPresentation;
            _isSubscribed = true;
            RefreshSelection();
            ApplyReadyPresentation();
        }

        private void UnsubscribeAndClear()
        {
            if (!_isSubscribed)
            {
                return;
            }

            _selectionChangeSource.OnActiveItemChanged -= RefreshSelection;
            _heldItemPrefabPool.OnPresentationAssetsReady -= ApplyReadyPresentation;
            _isSubscribed = false;
            _inputState.Cancel();
            HideSelection();
            _selectedItem = null;
            _selectedInstanceId = null;
        }

        private void RefreshSelection()
        {
            if (!_isSubscribed)
            {
                return;
            }

            IInteractionItem activeItem = _selectionChangeSource.ActiveItem;
            string instanceId = (_selectionChangeSource as IActiveItemInstanceSource)?.ActiveItemInstanceId;
            if (activeItem == _selectedItem && (activeItem == null ||
                !string.IsNullOrEmpty(instanceId) && _selectedInstanceId == instanceId))
            {
                return;
            }

            _selectedItem = activeItem;
            _selectedInstanceId = instanceId;
            _inputState.ChangeSelection();
            HideSelection();
            ShowSelectionWhenReady();
        }

        private void ShowSelectionWhenReady()
        {
            if (!_isSubscribed || _hasPresentedSelection || _selectedItem == null || !_heldItemPrefabPool.ArePresentationAssetsReady)
            {
                return;
            }

            _hasPresentedSelection = true;
            foreach (IItemSelectionPresentation selectionPresentation in _selectionPresentations)
            {
                selectionPresentation.ShowSelection(_selectedItem);
            }
        }

        private void ApplyReadyPresentation()
        {
            if (!_isSubscribed || !_heldItemPrefabPool.ArePresentationAssetsReady)
            {
                return;
            }

            if (_selectedItem == null)
            {
                ClearPresentation();
                return;
            }

            ShowSelectionWhenReady();
        }

        private void HideSelection()
        {
            if (!_hasPresentedSelection)
            {
                return;
            }

            _hasPresentedSelection = false;
            ClearPresentation();
        }

        private void ClearPresentation()
        {
            foreach (IItemSelectionPresentation selectionPresentation in _selectionPresentations)
            {
                selectionPresentation.HideSelection();
            }
        }

        #endregion
    }
}
