using System;
using System.Collections.Generic;
using FakeMG.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;

namespace FakeMG.InputHints
{
    /// <summary>
    /// Turns a hint into the glyphs of the controls that trigger it on the devices a glyph set draws. Each action shows its
    /// first binding those devices can perform, so list the preferred binding first in the action. A composite shows all
    /// its parts, and an action no device can perform shows nothing, which hides hints the player has no way to use.
    /// </summary>
    public sealed class InputGlyphResolver
    {
        /// <summary>Reading order of 2D-vector parts, so W/A/S/D reads as "W A S D" whatever order the bindings use.</summary>
        private static readonly string[] PART_READING_ORDER = { "up", "left", "down", "right" };

        private readonly HashSet<string> _reportedMissingGlyphs = new();

        #region Public Methods

        /// <param name="glyphs">Receives the glyphs of every action in the hint, in order.</param>
        public void Resolve(InputHint hint, InputGlyphSetSO glyphSetSO, List<InputGlyph> glyphs)
        {
            foreach (InputActionReference actionReference in hint.ActionReferences)
            {
                ResolveAction(actionReference.action, glyphSetSO, glyphs);
            }
        }

        #endregion

        #region Private Methods

        private void ResolveAction(InputAction action, InputGlyphSetSO glyphSetSO, List<InputGlyph> glyphs)
        {
            ReadOnlyArray<InputBinding> bindings = action.bindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                InputBinding binding = bindings[i];
                if (binding.isPartOfComposite)
                {
                    continue;
                }

                if (binding.isComposite)
                {
                    if (TryFindCompositeParts(bindings, i, glyphSetSO, out List<CompositePart> parts))
                    {
                        AddInReadingOrder(parts, glyphSetSO, glyphs);
                        return;
                    }

                    continue;
                }

                if (TryFindControl(binding.effectivePath, glyphSetSO, out InputControl control))
                {
                    glyphs.Add(ToGlyph(control, glyphSetSO));
                    return;
                }
            }
        }

        /// <returns>False unless every part has a control the glyph set draws.</returns>
        /// <remarks>A part bound several times, such as "up" to both W and the up arrow, shows its first binding only.</remarks>
        private static bool TryFindCompositeParts(
            ReadOnlyArray<InputBinding> bindings,
            int compositeIndex,
            InputGlyphSetSO glyphSetSO,
            out List<CompositePart> parts)
        {
            parts = new List<CompositePart>();
            var partNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var foundPartNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = compositeIndex + 1; i < bindings.Count && bindings[i].isPartOfComposite; i++)
            {
                string partName = bindings[i].name;
                partNames.Add(partName);
                if (!foundPartNames.Contains(partName) && TryFindControl(bindings[i].effectivePath, glyphSetSO, out InputControl control))
                {
                    foundPartNames.Add(partName);
                    parts.Add(new CompositePart(partName, control, parts.Count));
                }
            }

            return parts.Count > 0 && parts.Count == partNames.Count;
        }

        private void AddInReadingOrder(List<CompositePart> parts, InputGlyphSetSO glyphSetSO, List<InputGlyph> glyphs)
        {
            parts.Sort(CompareByReadingOrder);
            foreach (CompositePart part in parts)
            {
                glyphs.Add(ToGlyph(part.Control, glyphSetSO));
            }
        }

        /// <remarks>Parts outside the reading order, such as a modifier and its button, keep their binding order.</remarks>
        private static int CompareByReadingOrder(CompositePart first, CompositePart second)
        {
            int order = ReadingOrderOf(first.Name).CompareTo(ReadingOrderOf(second.Name));
            return order != 0 ? order : first.BindingOrder.CompareTo(second.BindingOrder);
        }

        private static int ReadingOrderOf(string partName)
        {
            int index = Array.FindIndex(PART_READING_ORDER, name => string.Equals(name, partName, StringComparison.OrdinalIgnoreCase));
            return index < 0 ? PART_READING_ORDER.Length : index;
        }

        private static bool TryFindControl(string bindingPath, InputGlyphSetSO glyphSetSO, out InputControl control)
        {
            foreach (InputDevice device in InputSystem.devices)
            {
                if (!glyphSetSO.Covers(device))
                {
                    continue;
                }

                control = InputControlPath.TryFindControl(device, bindingPath);
                if (control != null)
                {
                    return true;
                }
            }

            control = null;
            return false;
        }

        private InputGlyph ToGlyph(InputControl control, InputGlyphSetSO glyphSetSO)
        {
            string glyphKey = GlyphKeyOf(control);
            if (glyphSetSO.TryGetSprite(glyphKey, out Sprite sprite))
            {
                return new InputGlyph(sprite, string.Empty);
            }

            if (_reportedMissingGlyphs.Add($"{glyphSetSO.name}/{glyphKey}"))
            {
                Echo.Warning($"Glyph set '{glyphSetSO.name}' has no glyph for '{glyphKey}'; showing its name instead.", context: glyphSetSO);
            }

            return new InputGlyph(null, string.IsNullOrEmpty(control.shortDisplayName) ? control.displayName : control.shortDisplayName);
        }

        /// <remarks>
        /// Keys are named after their US-layout position, so on an AZERTY keyboard "q" prints "A". A key that prints a
        /// letter or digit is drawn by what it prints; other controls by their path on the device.
        /// </remarks>
        private static string GlyphKeyOf(InputControl control)
        {
            if (control is KeyControl key && key.displayName is { Length: 1 } printed && char.IsLetterOrDigit(printed[0]))
            {
                return printed.ToLowerInvariant();
            }

            return control.path.Substring(control.device.path.Length + 1);
        }

        #endregion

        private readonly struct CompositePart
        {
            public string Name { get; }
            public InputControl Control { get; }
            public int BindingOrder { get; }

            public CompositePart(string name, InputControl control, int bindingOrder)
            {
                Name = name;
                Control = control;
                BindingOrder = bindingOrder;
            }
        }
    }
}
