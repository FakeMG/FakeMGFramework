using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FakeMG.ActionMapManagement;
using FakeMG.Settings;
using UnityEngine.InputSystem;

namespace FakeMG.KeyBinding
{
    /// <summary>
    /// A rebind or a reset hands the slot's old key to whichever slot already used the new key in an action that can be live
    /// at the same time. A slot that had no key leaves that other slot empty.
    /// </summary>
    /// <remarks>
    /// Every action map is suspended while rebinding: the Input System can only rebind a disabled action, and menu actions
    /// such as Back must not react to the control being captured.
    /// </remarks>
    public sealed class KeyBindingService : IDisposable
    {
        private const string REBIND_SUSPENSION_REASON = "Rebinding a key";

        private readonly SettingsStateRepository _settings;
        private readonly IActionMapManager _actionMaps;
        private readonly KeyBindingConfigSO _configSO;
        private readonly KeyBindingConflictRules _conflictRules;
        private readonly KeyBindingSlotIndex _slotIndex;

        private InputActionRebindingExtensions.RebindingOperation _rebinding;
        private KeyBindingSlot _reboundSlot;
        private string _controlPathBeforeRebind;
        private IDisposable _inputSuspension;

        public bool IsRebinding => _rebinding != null;

        /// <summary>Raised once for every change to the bindings, whether made here or restored from the saved settings.</summary>
        public event Action OnBindingsChanged;
        public event Action<KeyBindingSlot> OnRebindStarted;
        public event Action<KeyBindingRebindResult> OnRebindFinished;

        public KeyBindingService(SettingsStateRepository settings, IActionMapManager actionMaps, KeyBindingConfigSO configSO)
        {
            _settings = settings;
            _actionMaps = actionMaps;
            _configSO = configSO;
            _conflictRules = new KeyBindingConflictRules(GetNeverLiveTogetherMapPairs(configSO.ActionMapConflictsPairsSO));
            _slotIndex = new KeyBindingSlotIndex(configSO);
        }

        #region Public Methods

        public void Dispose()
        {
            CancelRebind();
        }

        /// <returns>Empty when the family has no binding for the action.</returns>
        public IReadOnlyList<KeyBindingSlot> GetSlots(RebindableAction rebindableAction, BindingDeviceFamilySO deviceFamilySO)
        {
            return _slotIndex.GetSlots(rebindableAction, deviceFamilySO);
        }

        /// <summary>Loads saved overrides into the input actions.</summary>
        /// <remarks>Does nothing when they already hold these overrides, e.g. when the settings echo this service's own save.</remarks>
        public void ApplyOverrides(string overridesJson)
        {
            InputActionAsset inputActions = _configSO.InputActions;
            if (overridesJson == inputActions.SaveBindingOverridesAsJson())
            {
                return;
            }

            if (string.IsNullOrEmpty(overridesJson))
            {
                inputActions.RemoveAllBindingOverrides();
            }
            else
            {
                inputActions.LoadBindingOverridesFromJson(overridesJson);
            }

            OnBindingsChanged?.Invoke();
        }

        public void StartRebind(KeyBindingSlot slot)
        {
            if (IsRebinding)
            {
                return;
            }

            _reboundSlot = slot;
            _controlPathBeforeRebind = slot.ControlPath;
            _inputSuspension = _actionMaps.SuspendAllActionMaps(REBIND_SUSPENSION_REASON);

            BindingDeviceFamilySO deviceFamilySO = slot.DeviceFamilySO;
            _rebinding = slot.Action.PerformInteractiveRebinding(slot.BindingIndex)
                .WithCancelingThrough(deviceFamilySO.CancelControlPath)
                .OnMatchWaitForAnother(0f)
                .OnPotentialMatch(CompleteRebindWithRebindableControl);
            foreach (string excludedControlPath in deviceFamilySO.ExcludedControlPaths)
            {
                _rebinding.WithControlsExcluding(excludedControlPath);
            }

            _rebinding.OnComplete(CompleteRebind).OnCancel(FinishCancelledRebind).Start();
            OnRebindStarted?.Invoke(slot);
        }

        public void CancelRebind()
        {
            _rebinding?.Cancel();
        }

        /// <returns>
        /// The slot that took over this slot's previous key because it used the default one; null when none did, or while a
        /// rebind is in progress.
        /// </returns>
        public KeyBindingSlot ResetToDefault(KeyBindingSlot slot)
        {
            if (IsRebinding)
            {
                return null;
            }

            string controlPathBeforeReset = slot.ControlPath;
            slot.Action.RemoveBindingOverride(slot.BindingIndex);
            KeyBindingSlot swappedSlot = SwapConflictingSlot(slot, controlPathBeforeReset);
            SaveOverridesAndNotify();
            return swappedSlot;
        }

        public void ResetToDefaults(BindingDeviceFamilySO deviceFamilySO)
        {
            foreach (KeyBindingSlot slot in _slotIndex.GetFamilySlots(deviceFamilySO))
            {
                slot.Action.RemoveBindingOverride(slot.BindingIndex);
            }

            SaveOverridesAndNotify();
        }

        #endregion

        #region Private Methods

        private static IEnumerable<(string, string)> GetNeverLiveTogetherMapPairs(ActionMapConflictsPairsSO conflictPairsSO)
        {
            foreach (ConflictPair conflictPair in conflictPairsSO.ConflictPairs)
            {
                yield return (conflictPair.ActionMapA.ActionMapName, conflictPair.ActionMapB.ActionMapName);
            }
        }

        private void CompleteRebind(InputActionRebindingExtensions.RebindingOperation rebinding)
        {
            KeyBindingSlot reboundSlot = _reboundSlot;
            KeyBindingSlot swappedSlot = SwapConflictingSlot(reboundSlot, _controlPathBeforeRebind);
            EndRebind();

            SaveOverridesAndNotify();
            OnRebindFinished?.Invoke(new KeyBindingRebindResult(reboundSlot, swappedSlot, wasCancelled: false));
        }

        /// <remarks>
        /// Checked here, not passed as include paths: Unity already includes every device of the binding's control scheme, and
        /// one matching include path is enough, so the family's paths could only widen the search. With no wait after a match,
        /// nothing but this method completes the rebind.
        /// </remarks>
        private void CompleteRebindWithRebindableControl(InputActionRebindingExtensions.RebindingOperation rebinding)
        {
            BindingDeviceFamilySO deviceFamilySO = _reboundSlot.DeviceFamilySO;
            for (int i = rebinding.candidates.Count - 1; i >= 0; i--)
            {
                InputControl candidate = rebinding.candidates[i];
                if (!deviceFamilySO.IsRebindable(candidate))
                {
                    rebinding.RemoveCandidate(candidate);
                }
            }

            if (rebinding.candidates.Count > 0)
            {
                rebinding.Complete();
            }
        }

        private void FinishCancelledRebind(InputActionRebindingExtensions.RebindingOperation rebinding)
        {
            KeyBindingSlot reboundSlot = _reboundSlot;
            EndRebind();
            OnRebindFinished?.Invoke(new KeyBindingRebindResult(reboundSlot, swappedSlot: null, wasCancelled: true));
        }

        /// <returns>The slot that now holds the changed slot's previous key, or null when nothing conflicted.</returns>
        private KeyBindingSlot SwapConflictingSlot(KeyBindingSlot changedSlot, string previousControlPath)
        {
            KeyBindingSlot conflictingSlot = _conflictRules.FindConflictingSlot(_slotIndex.GetFamilySlots(changedSlot.DeviceFamilySO), changedSlot);
            conflictingSlot?.Action.ApplyBindingOverride(conflictingSlot.BindingIndex, previousControlPath);
            return conflictingSlot;
        }

        /// <remarks>
        /// The settings echo the save back through <see cref="ApplyOverrides"/>, which finds nothing to load, so this is the
        /// only notification for the change.
        /// </remarks>
        private void SaveOverridesAndNotify()
        {
            _settings.SetValue(_configSO.KeyBindingOverridesSettingSO, _configSO.InputActions.SaveBindingOverridesAsJson());
            OnBindingsChanged?.Invoke();
        }

        private void EndRebind()
        {
            _rebinding.Dispose();
            _rebinding = null;
            _reboundSlot = null;
            ReleaseInputSuspensionNextFrameAsync(_inputSuspension).Forget();
            _inputSuspension = null;
        }

        /// <remarks>
        /// The captured key press is still being processed this frame; re-enabling the maps now would let it trigger Back or
        /// whatever action now uses it.
        /// </remarks>
        private static async UniTaskVoid ReleaseInputSuspensionNextFrameAsync(IDisposable inputSuspension)
        {
            await UniTask.NextFrame();
            inputSuspension.Dispose();
        }

        #endregion
    }
}
