namespace FakeMG.KeyBinding
{
    public readonly struct KeyBindingRebindResult
    {
        public KeyBindingSlot ReboundSlot { get; }
        /// <summary>The slot that took over the rebound slot's previous key because it used the new one; null when none did.</summary>
        public KeyBindingSlot SwappedSlot { get; }
        public bool WasCancelled { get; }

        public KeyBindingRebindResult(KeyBindingSlot reboundSlot, KeyBindingSlot swappedSlot, bool wasCancelled)
        {
            ReboundSlot = reboundSlot;
            SwappedSlot = swappedSlot;
            WasCancelled = wasCancelled;
        }
    }
}
