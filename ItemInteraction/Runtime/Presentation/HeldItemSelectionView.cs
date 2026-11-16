using FakeMG.Framework;
using UnityEngine;
using VContainer;

namespace FakeMG.ItemInteraction
{
    public sealed class HeldItemSelectionView : MonoBehaviour, IItemSelectionPresentation
    {
        [SerializeField] private Transform _rightHand;
        [SerializeField] private Transform _leftHand;

        private HeldItemPrefabPool _heldItemPrefabPool;
        private IObjectResolver _objectResolver;
        private HeldItemPresentation _heldItemPresentation;
        private GameObject _heldItemInstance;

        public bool HasHeldItem => _heldItemInstance != null;

        public GameObject HeldItemInstance => _heldItemInstance;

        #region Public Methods

        public void Bind(
            HeldItemPrefabPool heldItemPrefabPool,
            IObjectResolver objectResolver,
            params IOffHandGripPresentation[] offHandGrips)
        {
            HideSelection();
            _heldItemPrefabPool = heldItemPrefabPool;
            _objectResolver = objectResolver;
            _heldItemPresentation = new HeldItemPresentation(offHandGrips);
        }

        /// <summary>
        /// Shows a held item in its holding hand. Items that are not held, such as the empty hand, show nothing.
        /// </summary>
        public void ShowSelection(IInteractionItem item)
        {
            HideSelection();
            if (item is not IHeldItem heldItem)
            {
                return;
            }

            _heldItemInstance = _heldItemPrefabPool.Instantiate(heldItem, GetHandSocket(heldItem.HoldingHand), _objectResolver);
            if (_heldItemInstance == null)
            {
                Echo.Warning($"Could not show {item.DisplayName} because its held prefab was unavailable.");
                return;
            }

            _heldItemPresentation.ApplyHandPoseAndBeginGrips(heldItem, _heldItemInstance);
        }

        public void HideSelection()
        {
            if (_heldItemInstance == null)
            {
                return;
            }

            _heldItemPresentation.ReleaseGrips();
            _heldItemInstance.SetActive(false);
            _heldItemPrefabPool.ReleaseInstance(_heldItemInstance);
            _heldItemInstance = null;
        }

        #endregion

        #region Private Methods

        private Transform GetHandSocket(HeldItemHand hand)
        {
            return hand == HeldItemHand.Left ? _leftHand : _rightHand;
        }

        #endregion
    }
}
