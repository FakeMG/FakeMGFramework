using System;
using System.Collections.Generic;

namespace FakeMG.ItemInteraction
{
    public interface ISelectedItemSource
    {
        IInteractionItem SelectedItem { get; }
    }

    public interface ISelectedItemInstanceSource : ISelectedItemSource
    {
        string SelectedItemInstanceId { get; }
    }

    public interface ISelectionChangeSource : ISelectedItemSource
    {
        event Action OnSelectedItemChanged;
    }

    public interface ISelectedItemConsumption
    {
        bool TryReserve(IInteractionItem expectedItem, int itemCount, out ISelectedItemConsumptionReservation consumption);
    }

    public interface ISelectedItemConsumptionReservation
    {
        bool TryCommit();
        void Cancel();
    }

    public interface IInteractionItemCatalog
    {
        IReadOnlyList<IInteractionItem> Items { get; }
    }

    public interface IInteractionInputBlocker
    {
        bool IsInputBlocked { get; }
        event Action<bool> OnInputBlockedChanged;
    }
}
