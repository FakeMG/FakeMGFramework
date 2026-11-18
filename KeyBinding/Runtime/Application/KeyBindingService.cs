using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FakeMG.ActionMapManagement;
using FakeMG.Framework;
using FakeMG.Settings;
using UnityEngine.InputSystem;
using VContainer.Unity;

namespace FakeMG.KeyBinding
{
    /// <summary>
    /// A rebind or a reset hands the slot's old key to whichever slot already used the new key in an action that can be live
    /// at the same time. A slot that had no key leaves that other slot empty.
    /// </summary>
    /// <remarks>
    /// Every action map is suspended while listening: the Input System can only rebind a disabled action, and menu actions
    /// such as Back must not react to the control being captured.
    /// </remarks>
    public sealed class KeyBindingService : IStartable, IDisposable
    {
        private const string REBIND_SUSPENSION_REASON = "Rebinding a key";

        private readonly SettingsStateRepository _settings;
        private readonly IActionMapManager _actionMaps;
        private readonly KeyBindingConfigSO _configSO;
        private readonly KeyBindingConflictRules _conflictRules;
        private readonly Dictionary<(RebindableAction, BindingDeviceFamilySO), List<KeyBindingSlot>> _slotsByActionAndFamily = new();
        private readonly Dictionary<BindingDeviceFamilySO, List<KeyBindingSlot>> _slotsByFamily = new();
        private readonly List<BoundKey> _boundKeysBuffer = new();

        private InputActionRebindingExtensions.RebindingOperation _rebinding;
        private KeyBindingSlot _reboundSlot;
        private string _controlPathBeforeRebind;
        private IDisposable _inputSuspension;

        public bool IsRebinding => _rebinding != null;

        public event Action OnBindingsChanged;
        public event Action<KeyBindingSlot> OnRebindStarted;
        public event Action<KeyBindingRebindResult> OnRebindFinished;

        public KeyBindingService(SettingsStateRepository settings, IActionMapManager actionMaps, KeyBindingConfigSO configSO)
        {
            _settings = settings;
            _actionMaps = actionMaps;
            _configSO = configSO;
            _conflictRules = new KeyBindingConflictRules(GetExclusiveMapPairs(configSO.ActionMapConflictsPairsSO));
        }

        #region Public Methods

        public void Start()
        {
            BuildSlots();
            _settings.RegisterSetting(_configSO.KeyBindingOverridesSettingSO);
            ApplyOverrides(_settings.GetValue(_configSO.KeyBindingOverridesSettingSO));
            _settings.Subscribe(_configSO.KeyBindingOverridesSettingSO, ApplyOverridesWhenSettingChanged);
        }

        public void Dispose()
        {
            _settings.Unsubscribe(_configSO.KeyBindingOverridesSettingSO, ApplyOverridesWhenSettingChanged);
            CancelRebind();
        }

        /// <returns>Empty when the family has no binding for the action.</returns>
        public IReadOnlyList<KeyBindingSlot> GetSlots(RebindableAction rebindableAction, BindingDeviceFamilySO deviceFamilySO)
        {
            return _slotsByActionAndFamily[(rebindableAction, deviceFamilySO)];
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

            _rebinding.OnComplete(CompleteRebind).OnCancel(CancelledRebind).Start();
            OnRebindStarted?.Invoke(slot);
        }

        public void CancelRebind()
        {
            _rebinding?.Cancel();
        }

        /// <returns>
        /// The slot that took over this slot's previous key because it used the default one; null when none did, or while a
        /// rebind is listening.
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
            SaveOverrides();
            return swappedSlot;
        }

        public void ResetToDefaults(BindingDeviceFamilySO deviceFamilySO)
        {
            foreach (KeyBindingSlot slot in _slotsByFamily[deviceFamilySO])
            {
                slot.Action.RemoveBindingOverride(slot.BindingIndex);
            }

            SaveOverrides();
            OnBindingsChanged?.Invoke();
        }

        #endregion

        #region Private Methods

        private static IEnumerable<(string, string)> GetExclusiveMapPairs(ActionMapConflictsPairsSO conflictPairsSO)
        {
            foreach (ConflictPair conflictPair in conflictPairsSO.ConflictPairs)
            {
                yield return (conflictPair.ActionMapA.ActionMapName, conflictPair.ActionMapB.ActionMapName);
            }
        }

        private void BuildSlots()
        {
            foreach (BindingDeviceFamilySO deviceFamilySO in _configSO.BindingDeviceFamilySOs)
            {
                List<KeyBindingSlot> familySlots = new();
                foreach (KeyBindingSection section in _configSO.KeyBindingCatalogSO.Sections)
                {
                    foreach (RebindableAction rebindableAction in section.Actions)
                    {
                        List<KeyBindingSlot> slots = FindSlots(rebindableAction, deviceFamilySO);
                        _slotsByActionAndFamily.Add((rebindableAction, deviceFamilySO), slots);
                        familySlots.AddRange(slots);
                    }
                }

                _slotsByFamily.Add(deviceFamilySO, familySlots);
            }
        }

        private static List<KeyBindingSlot> FindSlots(RebindableAction rebindableAction, BindingDeviceFamilySO deviceFamilySO)
        {
            List<KeyBindingSlot> slots = new(deviceFamilySO.SlotsPerAction);
            IReadOnlyList<InputBinding> bindings = rebindableAction.Action.bindings;
            for (int bindingIndex = 0; bindingIndex < bindings.Count && slots.Count < deviceFamilySO.SlotsPerAction; bindingIndex++)
            {
                InputBinding binding = bindings[bindingIndex];
                if (HasSlotShape(rebindableAction, binding) && deviceFamilySO.Owns(binding))
                {
                    slots.Add(new KeyBindingSlot(rebindableAction, deviceFamilySO, bindingIndex));
                }
            }

            if (slots.Count > 0 && slots.Count < deviceFamilySO.SlotsPerAction)
            {
                Echo.Warning(
                    $"'{rebindableAction.Label}' has {slots.Count} of {deviceFamilySO.SlotsPerAction} {deviceFamilySO.DisplayName} " +
                    $"bindings, so its other slots are inert. Add an empty binding in the '{deviceFamilySO.BindingGroup}' group.");
            }

            return slots;
        }

        private static bool HasSlotShape(RebindableAction rebindableAction, InputBinding binding)
        {
            return rebindableAction.IsCompositePart
                ? binding.isPartOfComposite && string.Equals(binding.name, rebindableAction.CompositePartName, StringComparison.OrdinalIgnoreCase)
                : !binding.isComposite && !binding.isPartOfComposite;
        }

        private void ApplyOverridesWhenSettingChanged(SettingDefinitionGenericSO<string> settingSO, string overridesJson)
        {
            ApplyOverrides(overridesJson);
        }

        private void ApplyOverrides(string overridesJson)
        {
            InputActionAsset inputActions = _configSO.InputActions;
            if (string.IsNullOrEmpty(overridesJson))
            {
                inputActions.RemoveAllBindingOverrides();
            }
            else if (overridesJson != inputActions.SaveBindingOverridesAsJson())
            {
                inputActions.LoadBindingOverridesFromJson(overridesJson);
            }

            OnBindingsChanged?.Invoke();
        }

        private void CompleteRebind(InputActionRebindingExtensions.RebindingOperation rebinding)
        {
            KeyBindingSlot reboundSlot = _reboundSlot;
            KeyBindingSlot swappedSlot = SwapConflictingSlot(reboundSlot, _controlPathBeforeRebind);
            EndRebind();

            SaveOverrides();
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

        private void CancelledRebind(InputActionRebindingExtensions.RebindingOperation rebinding)
        {
            KeyBindingSlot reboundSlot = _reboundSlot;
            EndRebind();
            OnRebindFinished?.Invoke(new KeyBindingRebindResult(reboundSlot, swappedSlot: null, wasCancelled: true));
        }

        /// <returns>The slot that now holds the changed slot's previous key, or null when nothing clashed.</returns>
        private KeyBindingSlot SwapConflictingSlot(KeyBindingSlot changedSlot, string previousControlPath)
        {
            List<KeyBindingSlot> familySlots = _slotsByFamily[changedSlot.DeviceFamilySO];
            _boundKeysBuffer.Clear();
            foreach (KeyBindingSlot slot in familySlots)
            {
                _boundKeysBuffer.Add(new BoundKey(slot.ActionMapName, slot.ControlPath));
            }

            int changedSlotIndex = familySlots.IndexOf(changedSlot);
            int conflictingSlotIndex = _conflictRules.FindConflictingSlotIndex(_boundKeysBuffer, changedSlotIndex, changedSlot.ControlPath);
            if (conflictingSlotIndex == KeyBindingConflictRules.NO_CONFLICT)
            {
                return null;
            }

            KeyBindingSlot conflictingSlot = familySlots[conflictingSlotIndex];
            conflictingSlot.Action.ApplyBindingOverride(conflictingSlot.BindingIndex, previousControlPath);
            return conflictingSlot;
        }

        private void SaveOverrides()
        {
            _settings.SetValue(_configSO.KeyBindingOverridesSettingSO, _configSO.InputActions.SaveBindingOverridesAsJson());
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
