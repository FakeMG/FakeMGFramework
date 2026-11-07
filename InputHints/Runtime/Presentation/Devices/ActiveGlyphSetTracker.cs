using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using VContainer.Unity;

namespace FakeMG.InputHints
{
    /// <summary>
    /// Follows the device the player pressed last and picks the glyph set that draws it, so hints switch between keyboard,
    /// Xbox, PlayStation and other families as the player switches. Falls back to the most recently used device that is
    /// still connected when the followed one is unplugged.
    /// </summary>
    public sealed class ActiveGlyphSetTracker : IInitializable, IDisposable
    {
        private readonly InputGlyphLibrarySO _glyphLibrarySO;
        private IDisposable _buttonPressListener;

        /// <remarks>Null while no connected device has a glyph set.</remarks>
        public InputGlyphSetSO ActiveSetSO { get; private set; }

        /// <summary>The active set changed, or a device it draws joined or left, so which bindings can show may differ.</summary>
        public event Action OnGlyphsChanged;

        public ActiveGlyphSetTracker(InputGlyphLibrarySO glyphLibrarySO)
        {
            _glyphLibrarySO = glyphLibrarySO;
        }

        #region Public Methods

        public void Initialize()
        {
            ActiveSetSO = FindSetForMostRecentDevice();
            _buttonPressListener = InputSystem.onAnyButtonPress.Call(FollowPressedDevice);
            InputSystem.onDeviceChange += FollowDeviceChange;
        }

        public void Dispose()
        {
            _buttonPressListener?.Dispose();
            _buttonPressListener = null;
            InputSystem.onDeviceChange -= FollowDeviceChange;
        }

        #endregion

        #region Private Methods

        private InputGlyphSetSO FindSetForMostRecentDevice()
        {
            InputGlyphSetSO mostRecentSetSO = null;
            double mostRecentUpdateTimeSeconds = double.MinValue;
            foreach (InputDevice device in InputSystem.devices)
            {
                if (device.lastUpdateTime > mostRecentUpdateTimeSeconds && _glyphLibrarySO.TryFindSetFor(device, out InputGlyphSetSO setSO))
                {
                    mostRecentSetSO = setSO;
                    mostRecentUpdateTimeSeconds = device.lastUpdateTime;
                }
            }

            return mostRecentSetSO;
        }

        private void FollowPressedDevice(InputControl control)
        {
            if (_glyphLibrarySO.TryFindSetFor(control.device, out InputGlyphSetSO setSO))
            {
                ShowSet(setSO);
            }
        }

        /// <remarks>Falls back to another device's set once the last device the active set draws leaves.</remarks>
        private void FollowDeviceChange(InputDevice device, InputDeviceChange change)
        {
            bool hasJoined = change is InputDeviceChange.Added or InputDeviceChange.Reconnected;
            bool hasLeft = change is InputDeviceChange.Removed or InputDeviceChange.Disconnected;
            if (!hasJoined && !hasLeft)
            {
                return;
            }

            if (!ActiveSetSO || (hasLeft && !IsAnyConnectedDeviceDrawnBy(ActiveSetSO)))
            {
                ShowSet(FindSetForMostRecentDevice());
            }
            else if (ActiveSetSO.Covers(device))
            {
                OnGlyphsChanged?.Invoke();
            }
        }

        private static bool IsAnyConnectedDeviceDrawnBy(InputGlyphSetSO setSO)
        {
            foreach (InputDevice device in InputSystem.devices)
            {
                if (setSO.Covers(device))
                {
                    return true;
                }
            }

            return false;
        }

        private void ShowSet(InputGlyphSetSO setSO)
        {
            if (ActiveSetSO == setSO)
            {
                return;
            }

            ActiveSetSO = setSO;
            OnGlyphsChanged?.Invoke();
        }

        #endregion
    }
}
