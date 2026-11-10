using System.Collections.Generic;
using FakeMG.Framework;
using UnityEngine;
using VContainer;

namespace FakeMG.ItemInteraction
{
    public sealed class HeldItemSelectionView : MonoBehaviour, IItemSelectionPresentation
    {
        [SerializeField] private Transform _rightHand;

        private HeldItemPrefabPool _heldItemPrefabPool;
        private HeldItemPresentation _heldItemPresentation;
        private GameObject _heldItemInstance;

        #region Public Methods

        [Inject]
        public void Construct(HeldItemPrefabPool heldItemPrefabPool, IReadOnlyList<IHeldItemSelectionObserver> selectionObservers)
        {
            _heldItemPrefabPool = heldItemPrefabPool;
            _heldItemPresentation = new HeldItemPresentation(selectionObservers);
        }

        public void ShowSelection(IInteractionItem item)
        {
            if (item is not IUsableItem usableItem)
            {
                return;
            }

            HideSelection();
            _heldItemInstance = _heldItemPrefabPool.Instantiate(usableItem, _rightHand);
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
            Destroy(_heldItemInstance);
            _heldItemInstance = null;
        }

        #endregion
    }
}
