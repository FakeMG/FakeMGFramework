using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace FakeMG.ItemInteraction
{
    public sealed class SelectionLifecycleSubscriber : MonoBehaviour
    {
        private ISelectionChangeSource _selectionChangeSource;
        private HeldItemPrefabPool _heldItemPrefabPool;
        private InteractionInputState _inputState;
        private IReadOnlyList<IItemSelectionPresentation> _selectionPresentations;
        private bool _isInjected;
        private bool _isSubscribed;
        private IInteractionItem _presentedItem;
        private string _presentedInstanceId;

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (_isInjected)
            {
                Subscribe();
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        #endregion

        #region Public Methods

        [Inject]
        public void Construct(
            ISelectionChangeSource selectionChangeSource,
            HeldItemPrefabPool heldItemPrefabPool,
            InteractionInputState inputState,
            IReadOnlyList<IItemSelectionPresentation> selectionPresentations)
        {
            _selectionChangeSource = selectionChangeSource;
            _heldItemPrefabPool = heldItemPrefabPool;
            _inputState = inputState;
            _selectionPresentations = selectionPresentations;
            _isInjected = true;

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

            _selectionChangeSource.OnActiveItemChanged += ApplySelectionWhenPresentationAssetsAreReady;
            _heldItemPrefabPool.OnPresentationAssetsReady += ApplySelectionPresentation;
            _isSubscribed = true;
            ApplySelectionWhenPresentationAssetsAreReady();
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed)
            {
                return;
            }

            _selectionChangeSource.OnActiveItemChanged -= ApplySelectionWhenPresentationAssetsAreReady;
            _heldItemPrefabPool.OnPresentationAssetsReady -= ApplySelectionPresentation;
            _isSubscribed = false;
        }

        private void ApplySelectionWhenPresentationAssetsAreReady()
        {
            if (_heldItemPrefabPool.ArePresentationAssetsReady)
            {
                ApplySelectionPresentation();
            }
        }

        private void ApplySelectionPresentation()
        {
            IInteractionItem activeItem = _selectionChangeSource.ActiveItem;
            string instanceId = (_selectionChangeSource as IActiveItemInstanceSource)?.ActiveItemInstanceId;
            if (!string.IsNullOrEmpty(instanceId) && _presentedItem == activeItem && _presentedInstanceId == instanceId)
            {
                return;
            }

            _presentedItem = activeItem;
            _presentedInstanceId = instanceId;
            _inputState.ChangeSelection();
            foreach (IItemSelectionPresentation selectionPresentation in _selectionPresentations)
            {
                selectionPresentation.HideSelection();
            }

            if (activeItem == null)
            {
                return;
            }

            foreach (IItemSelectionPresentation selectionPresentation in _selectionPresentations)
            {
                selectionPresentation.ShowSelection(activeItem);
            }
        }

        #endregion
    }
}
