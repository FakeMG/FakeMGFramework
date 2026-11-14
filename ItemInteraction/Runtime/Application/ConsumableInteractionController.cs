using System.Collections.Generic;
using System.Linq;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.ItemInteraction
{
    public sealed class ConsumableInteractionController : IInteractionController
    {
        private readonly ISelectedItemSource _selectedItemSource;
        private readonly ISelectedItemConsumption _selectedItemConsumption;
        private readonly IConsumableEffect _consumableEffect;
        private readonly IReadOnlyList<IItemUsePresentation> _usePresentations;

        private IConsumableItem _activeConsumableItem;
        private ItemActionSO _activeActionSO;
        private TimedItemActionState _actionState;

        public bool IsInteractionActive => _actionState?.IsActive == true;

        public ConsumableInteractionController(
            ISelectedItemSource selectedItemSource,
            ISelectedItemConsumption selectedItemConsumption,
            IConsumableEffect consumableEffect,
            IReadOnlyList<IItemUsePresentation> usePresentations)
        {
            _selectedItemSource = selectedItemSource;
            _selectedItemConsumption = selectedItemConsumption;
            _consumableEffect = consumableEffect;
            _usePresentations = usePresentations;
        }

        #region Public Methods

        public bool Supports(IInteractionItem selectedItem, ItemActionSO actionSO)
        {
            return selectedItem is IConsumableItem consumableItem &&
                   actionSO != null && consumableItem.Actions.Contains(actionSO);
        }

        public bool TryBeginInteraction(
            IInteractionItem selectedItem,
            ItemActionSO actionSO,
            Ray targetRay,
            Vector3 interactionOriginPositionMeters,
            Vector2 screenPositionPixels)
        {
            if (selectedItem is not IConsumableItem consumableItem
                || actionSO == null
                || _selectedItemSource.SelectedItem != consumableItem
                || !_consumableEffect.CanApply(consumableItem))
            {
                Echo.Warning("Cannot begin consumable interaction because selection, action, or effect validation failed.");
                return false;
            }

            _activeConsumableItem = consumableItem;
            _activeActionSO = actionSO;
            ItemAnimationProfileSO animationProfileSO = actionSO.AnimationProfileSO;
            _actionState = new TimedItemActionState(
                actionSO.InputLifecycleSO,
                animationProfileSO.ActionMomentSeconds,
                animationProfileSO.UseDurationSeconds);

            foreach (IItemUsePresentation usePresentation in _usePresentations)
            {
                usePresentation.BeginUse(consumableItem, actionSO, playbackSpeedMultiplier: 1f);
            }
            return true;
        }

        public void MovePointer(Ray targetRay, Vector2 screenPositionPixels)
        {
        }

        public void Tick(float deltaSeconds)
        {
            if (_actionState == null)
            {
                return;
            }

            TimedItemActionOutcome outcome = _actionState.Tick(deltaSeconds);
            if (outcome.ShouldAttemptCommit)
            {
                TryCommitConsumption();
            }

            CompletePresentationWhenFinished();
        }

        public void ReleaseInteraction()
        {
            _actionState?.Release();
            CompletePresentationWhenFinished();
        }

        public void CancelInteraction()
        {
            _actionState?.Cancel();
            FinishPresentation();
        }

        #endregion

        #region Private Methods

        private void TryCommitConsumption()
        {
            if (_selectedItemSource.SelectedItem != _activeConsumableItem ||
                !_consumableEffect.CanApply(_activeConsumableItem))
            {
                Echo.Warning($"Canceled {_activeConsumableItem.DisplayName} at its action moment because it is no longer valid.");
                return;
            }

            if (!_selectedItemConsumption.TryReserve(_activeConsumableItem, 1, out ISelectedItemConsumptionReservation consumption))
            {
                Echo.Warning($"Could not reserve one {_activeConsumableItem.DisplayName} at its action moment.");
                return;
            }

            if (!_consumableEffect.TryApply(_activeConsumableItem))
            {
                Echo.Warning($"The effect for {_activeConsumableItem.DisplayName} rejected application; the item was preserved.");
                consumption.Cancel();
                return;
            }

            if (!consumption.TryCommit())
            {
                Echo.Error($"The effect for {_activeConsumableItem.DisplayName} was applied, but its reserved item unit could not be consumed.");
            }
        }

        private void CompletePresentationWhenFinished()
        {
            if (_actionState?.IsActive == false)
            {
                FinishPresentation();
            }
        }

        private void FinishPresentation()
        {
            if (_activeConsumableItem != null && _activeActionSO != null)
            {
                foreach (IItemUsePresentation usePresentation in _usePresentations)
                {
                    usePresentation.EndUse(_activeConsumableItem, _activeActionSO);
                }
            }

            _activeConsumableItem = null;
            _activeActionSO = null;
            _actionState = null;
        }

        #endregion
    }
}
