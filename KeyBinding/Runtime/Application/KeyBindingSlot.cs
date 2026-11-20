using UnityEngine.InputSystem;

namespace FakeMG.KeyBinding
{
    public sealed class KeyBindingSlot : IBoundKey
    {
        public RebindableAction RebindableAction { get; }
        public BindingDeviceFamilySO DeviceFamilySO { get; }
        public int BindingIndex { get; }

        public InputAction Action => RebindableAction.Action;
        public string ControlPath => Action.bindings[BindingIndex].effectivePath;
        public bool IsUnbound => string.IsNullOrEmpty(ControlPath);
        public string ActionMapName => Action.actionMap.name;

        public KeyBindingSlot(RebindableAction rebindableAction, BindingDeviceFamilySO deviceFamilySO, int bindingIndex)
        {
            RebindableAction = rebindableAction;
            DeviceFamilySO = deviceFamilySO;
            BindingIndex = bindingIndex;
        }

        #region Public Methods

        /// <remarks>
        /// Names the control on the first connected device the binding resolves to, not the device in use: with a PlayStation
        /// and a Switch pad connected, buttonNorth may read "Triangle" while the player holds the Switch pad. Prefer the
        /// active glyph set's glyph wherever a pad is involved.
        /// </remarks>
        public string GetDisplayText()
        {
            return Action.GetBindingDisplayString(BindingIndex, InputBinding.DisplayStringOptions.DontIncludeInteractions);
        }

        #endregion
    }
}
