using System;

namespace FakeMG.ItemInteraction
{
    public interface IActiveItemSource
    {
        IInteractionItem ActiveItem { get; }
    }

    public interface IActiveItemInstanceSource : IActiveItemSource
    {
        string ActiveItemInstanceId { get; }
    }

    public interface ISelectionChangeSource : IActiveItemSource
    {
        event Action OnActiveItemChanged;
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
        System.Collections.Generic.IReadOnlyList<IInteractionItem> Items { get; }
    }

    public interface IInteractionInputBlocker
    {
        bool IsInputBlocked { get; }
        event Action<bool> OnInputBlockedChanged;
    }
}
