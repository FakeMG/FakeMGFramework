namespace FakeMG.ItemInteraction
{
    /// <summary>
    /// Shows a character using its selected item. The item is never null: an empty hand is its owner's empty-hand item.
    /// </summary>
    public interface IItemUsePresentation
    {
        /// <summary>
        /// Plays the action's use sequence from its first frame, even when its clip is still fading out from the previous use.
        /// </summary>
        /// <param name="playbackSpeedMultiplier">
        /// Stretches the sequence, for example so its action moment lands on a gameplay moment. 1 plays it as authored.
        /// </param>
        void BeginUse(IInteractionItem item, ItemActionSO actionSO, float playbackSpeedMultiplier);

        void EndUse(IInteractionItem item, ItemActionSO actionSO);
    }
}
