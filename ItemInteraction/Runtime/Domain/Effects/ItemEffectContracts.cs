using System;

namespace FakeMG.ItemInteraction
{
    public interface IConsumableEffect
    {
        bool CanApply(IConsumableItem consumableItem);
        bool TryApply(IConsumableItem consumableItem);
    }

    public sealed class ConsumableEffectSignal : IConsumableEffect
    {
        public event Action<IConsumableItem> OnConsumableEffectApplied;

        public bool CanApply(IConsumableItem consumableItem)
        {
            return true;
        }

        public bool TryApply(IConsumableItem consumableItem)
        {
            OnConsumableEffectApplied?.Invoke(consumableItem);
            return true;
        }
    }

}
