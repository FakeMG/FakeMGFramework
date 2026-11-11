using FakeMG.Framework;
using UnityEngine;
using VContainer;

namespace FakeMG.ItemInteraction
{
    public sealed class HeldItemSelectionView : MonoBehaviour, IItemSelectionPresentation
    {
        [SerializeField] private Transform _rightHand;

        private HeldItemPrefabPool _heldItemPrefabPool;
        private IObjectResolver _objectResolver;
        private HeldItemPresentation _heldItemPresentation;
        private GameObject _heldItemInstance;

        public bool HasHeldItem => _heldItemInstance != null;

        #region Public Methods

        public void Bind(HeldItemPrefabPool heldItemPrefabPool, IObjectResolver objectResolver,
            params IHeldItemSelectionObserver[] selectionObservers)
        {
            HideSelection();
            _heldItemPrefabPool = heldItemPrefabPool;
            _objectResolver = objectResolver;
            _heldItemPresentation = new HeldItemPresentation(selectionObservers);
        }

        public void ShowSelection(IInteractionItem item)
        {
            HideSelection();
            if (item is not IUsableItem usableItem)
            {
                return;
            }

            _heldItemInstance = _heldItemPrefabPool.Instantiate(usableItem, _rightHand, _objectResolver);
            if (_heldItemInstance == null)
            {
                Echo.Warning($"Could not show {item.DisplayName} because its held prefab was unavailable.");
                return;
            }

            _heldItemPresentation.ApplyHandPoseAndNotifyShown(usableItem, _heldItemInstance);
        }

        public void HideSelection()
        {
            if (_heldItemInstance == null)
            {
                return;
            }

            _heldItemPresentation.NotifyHidden(_heldItemInstance);

            _heldItemInstance.SetActive(false);
            _heldItemPrefabPool.ReleaseInstance(_heldItemInstance);
            _heldItemInstance = null;
        }

        #endregion
    }
}
