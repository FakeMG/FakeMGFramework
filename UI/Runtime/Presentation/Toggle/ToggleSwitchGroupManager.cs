using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.Framework.UI.Toggle
{
    public class ToggleSwitchGroupManager : MonoBehaviour
    {
        [Header("Start Value")]
        [SerializeField] private ToggleSwitch _initialToggleSwitch;

        [Header("Toggle Options")]
        [SerializeField] private bool _allCanBeToggledOff;

        private readonly List<ToggleSwitch> _toggleSwitches = new();

        private void Awake()
        {
            ToggleSwitch[] toggleSwitches = GetComponentsInChildren<ToggleSwitch>();
            foreach (var toggleSwitch in toggleSwitches)
            {
                RegisterToggleButtonToGroup(toggleSwitch);
            }
        }

        private void RegisterToggleButtonToGroup(ToggleSwitch toggleSwitch)
        {
            if (_toggleSwitches.Contains(toggleSwitch))
                return;

            _toggleSwitches.Add(toggleSwitch);

            toggleSwitch.SetupForManager(this);
        }

        private void Start()
        {
            bool areAllToggledOff = true;
            foreach (var button in _toggleSwitches)
            {
                if (!button.IsOn)
                    continue;

                areAllToggledOff = false;
                break;
            }

            if (!areAllToggledOff || _allCanBeToggledOff) return;

            if (_initialToggleSwitch != null)
                _initialToggleSwitch.ToggleByGroupManager(true);
            else
                _toggleSwitches[0].ToggleByGroupManager(true);
        }

        public void ToggleGroup(ToggleSwitch toggleSwitch)
        {
            if (_toggleSwitches.Count <= 1)
                return;

            if (_allCanBeToggledOff && toggleSwitch.IsOn)
            {
                foreach (var button in _toggleSwitches)
                {
                    if (button == null)
                        continue;

                    button.ToggleByGroupManager(false);
                }
            }
            else
            {
                foreach (var button in _toggleSwitches)
                {
                    if (button == null)
                        continue;

                    if (button == toggleSwitch)
                        button.ToggleByGroupManager(true);
                    else
                        button.ToggleByGroupManager(false);
                }
            }
        }
    }
}