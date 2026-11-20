namespace FakeMG.KeyBinding
{
    /// <summary>The status line's text for each key binding outcome.</summary>
    /// <remarks>
    /// Messages name actions, not their keys: a pad button's name comes from whichever matching pad connected first, so with
    /// several brands connected it can name the wrong one. The rebound slots show the right glyph instead.
    /// </remarks>
    public static class KeyBindingStatusMessages
    {
        private const string SWAPPED_MESSAGE_FORMAT = "{0} swapped bindings with {1}.";
        private const string TAKEN_MESSAGE_FORMAT = "{0} took that binding from {1}, which is now unbound.";
        private const string FAMILY_RESET_MESSAGE_FORMAT = "{0} bindings reset to defaults.";
        private const string SLOT_RESET_MESSAGE_FORMAT = "{0} reset to its default binding.";
        private const string REBIND_STARTED_MESSAGE_FORMAT = "{0} for {1}. {2} cancels.";
        private const string REBOUND_MESSAGE_FORMAT = "{0} rebound.";
        private const string REBIND_CANCELLED_MESSAGE = "Kept the previous binding.";

        #region Public Methods

        public static string ForRebindStarted(KeyBindingSlot slot)
        {
            BindingDeviceFamilySO deviceFamilySO = slot.DeviceFamilySO;
            return string.Format(REBIND_STARTED_MESSAGE_FORMAT, deviceFamilySO.RebindPrompt, slot.RebindableAction.Label, deviceFamilySO.CancelControlName);
        }

        public static string ForRebindResult(KeyBindingRebindResult result)
        {
            if (result.WasCancelled)
            {
                return REBIND_CANCELLED_MESSAGE;
            }

            string reboundMessage = string.Format(REBOUND_MESSAGE_FORMAT, result.ReboundSlot.RebindableAction.Label);
            return ForKeyChange(result.ReboundSlot, result.SwappedSlot, reboundMessage);
        }

        /// <param name="swappedSlot">The slot that took the reset slot's old key; null when none did.</param>
        public static string ForSlotReset(KeyBindingSlot resetSlot, KeyBindingSlot swappedSlot)
        {
            string resetMessage = string.Format(SLOT_RESET_MESSAGE_FORMAT, resetSlot.RebindableAction.Label);
            return ForKeyChange(resetSlot, swappedSlot, resetMessage);
        }

        public static string ForFamilyReset(BindingDeviceFamilySO deviceFamilySO)
        {
            return string.Format(FAMILY_RESET_MESSAGE_FORMAT, deviceFamilySO.DisplayName);
        }

        #endregion

        #region Private Methods

        /// <param name="unswappedMessage">Returned when no other slot changed.</param>
        private static string ForKeyChange(KeyBindingSlot changedSlot, KeyBindingSlot swappedSlot, string unswappedMessage)
        {
            if (swappedSlot == null)
            {
                return unswappedMessage;
            }

            string messageFormat = swappedSlot.IsUnbound ? TAKEN_MESSAGE_FORMAT : SWAPPED_MESSAGE_FORMAT;
            return string.Format(messageFormat, changedSlot.RebindableAction.Label, swappedSlot.RebindableAction.Label);
        }

        #endregion
    }
}
