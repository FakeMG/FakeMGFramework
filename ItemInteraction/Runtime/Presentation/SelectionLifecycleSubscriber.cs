using UnityEngine;

namespace FakeMG.ItemInteraction
{
    /// <summary>
    /// Subscribes a character's <see cref="SelectionPresenter"/> to its selection and held-prefab preload while enabled.
    /// </summary>
    public sealed class SelectionLifecycleSubscriber : MonoBehaviour
    {
        private ISelectionChangeSource _selectionChangeSource;
        private HeldItemPrefabPool _heldItemPrefabPool;
        private SelectionPresenter _selectionPresenter;

        private bool IsBound => _selectionPresenter != null;

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (IsBound)
            {
                Subscribe();
            }
        }

        private void OnDisable()
        {
            if (IsBound)
            {
                UnsubscribeAndClear();
            }
        }

        #endregion

        #region Public Methods

        public void Bind(
            ISelectionChangeSource selectionChangeSource,
            HeldItemPrefabPool heldItemPrefabPool,
            IInteractionSelectionLifecycle interactionLifecycle,
            params IItemSelectionPresentation[] selectionPresentations)
        {
            if (IsBound && isActiveAndEnabled)
            {
                UnsubscribeAndClear();
            }

            _selectionChangeSource = selectionChangeSource;
            _heldItemPrefabPool = heldItemPrefabPool;
            _selectionPresenter = new SelectionPresenter(
                selectionChangeSource,
                heldItemPrefabPool,
                interactionLifecycle,
                selectionPresentations);

            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        #endregion

        #region Private Methods

        private void Subscribe()
        {
            _selectionChangeSource.OnSelectedItemChanged += PresentSelectedItem;
            _heldItemPrefabPool.OnPresentationAssetsReady += ShowSelectionWhenReady;
            _selectionPresenter.PresentSelectedItem();
        }

        private void UnsubscribeAndClear()
        {
            _selectionChangeSource.OnSelectedItemChanged -= PresentSelectedItem;
            _heldItemPrefabPool.OnPresentationAssetsReady -= ShowSelectionWhenReady;
            _selectionPresenter.CancelAndClear();
        }

        private void PresentSelectedItem()
        {
            _selectionPresenter.PresentSelectedItem();
        }

        private void ShowSelectionWhenReady()
        {
            _selectionPresenter.ShowSelectionWhenReady();
        }

        #endregion
    }
}
