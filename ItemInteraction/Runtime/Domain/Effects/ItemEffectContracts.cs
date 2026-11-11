namespace FakeMG.ItemInteraction
{
    public interface IConsumableEffect
    {
        bool CanApply(IConsumableItem consumableItem);
        bool TryApply(IConsumableItem consumableItem);
    }

}
