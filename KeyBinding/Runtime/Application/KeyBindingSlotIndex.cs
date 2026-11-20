using System;
using System.Collections.Generic;
using FakeMG.Framework;
using UnityEngine.InputSystem;

namespace FakeMG.KeyBinding
{
    /// <summary>Every rebindable binding of the catalog, found once per device family.</summary>
    public sealed class KeyBindingSlotIndex
    {
        private readonly Dictionary<(RebindableAction, BindingDeviceFamilySO), List<KeyBindingSlot>> _slotsByActionAndFamily = new();
        private readonly Dictionary<BindingDeviceFamilySO, List<KeyBindingSlot>> _slotsByFamily = new();
        private readonly List<string> _missingSlotReports = new();

        /// <summary>One report per action that lacks some of its family's slots. Those slots cannot be rebound.</summary>
        public IReadOnlyList<string> MissingSlotReports => _missingSlotReports;

        public KeyBindingSlotIndex(KeyBindingConfigSO configSO)
        {
            foreach (BindingDeviceFamilySO deviceFamilySO in configSO.BindingDeviceFamilySOs)
            {
                List<KeyBindingSlot> familySlots = new();
                foreach (KeyBindingSection section in configSO.KeyBindingCatalogSO.Sections)
                {
                    foreach (RebindableAction rebindableAction in section.Actions)
                    {
                        List<KeyBindingSlot> slots = FindSlotsAndReportMissing(rebindableAction, deviceFamilySO);
                        _slotsByActionAndFamily.Add((rebindableAction, deviceFamilySO), slots);
                        familySlots.AddRange(slots);
                    }
                }

                _slotsByFamily.Add(deviceFamilySO, familySlots);
            }
        }

        #region Public Methods

        /// <returns>Empty when the family has no binding for the action.</returns>
        public IReadOnlyList<KeyBindingSlot> GetSlots(RebindableAction rebindableAction, BindingDeviceFamilySO deviceFamilySO)
        {
            return _slotsByActionAndFamily[(rebindableAction, deviceFamilySO)];
        }

        public IReadOnlyList<KeyBindingSlot> GetFamilySlots(BindingDeviceFamilySO deviceFamilySO)
        {
            return _slotsByFamily[deviceFamilySO];
        }

        #endregion

        #region Private Methods

        /// <remarks>
        /// Slots are the bindings the asset authors, empty ones included: a slot the asset lacks cannot be rebound, so it is
        /// reported, and warned about, for the binding to be added.
        /// </remarks>
        private List<KeyBindingSlot> FindSlotsAndReportMissing(RebindableAction rebindableAction, BindingDeviceFamilySO deviceFamilySO)
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
                string report = $"'{rebindableAction.Label}' has {slots.Count} of {deviceFamilySO.SlotsPerAction} {deviceFamilySO.DisplayName} " +
                                $"bindings, so its other slots are inert. Add an empty binding in the '{deviceFamilySO.BindingGroup}' group.";
                _missingSlotReports.Add(report);
                Echo.Warning(report);
            }

            return slots;
        }

        private static bool HasSlotShape(RebindableAction rebindableAction, InputBinding binding)
        {
            return rebindableAction.IsCompositePart
                ? binding.isPartOfComposite && string.Equals(binding.name, rebindableAction.CompositePartName, StringComparison.OrdinalIgnoreCase)
                : !binding.isComposite && !binding.isPartOfComposite;
        }

        #endregion
    }
}
