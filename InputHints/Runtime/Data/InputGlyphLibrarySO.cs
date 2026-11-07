using System.Collections.Generic;
using FakeMG.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FakeMG.InputHints
{
    /// <summary>Every glyph set a game ships, and which one draws a given device.</summary>
    [CreateAssetMenu(menuName = FakeMGEditorMenus.INPUT_HINTS + "/Input Glyph Library SO")]
    public sealed class InputGlyphLibrarySO : ScriptableObject
    {
        [Header("Glyph Sets")]
        [Tooltip("Checked in order and the first set covering the device wins, so list specific families (PlayStation, " +
                 "Switch) before broad ones (any Gamepad).")]
        [SerializeField] private List<InputGlyphSetSO> _glyphSetSOs = new();

        #region Public Methods

        public bool TryFindSetFor(InputDevice device, out InputGlyphSetSO glyphSetSO)
        {
            foreach (InputGlyphSetSO candidateSO in _glyphSetSOs)
            {
                if (candidateSO.Covers(device))
                {
                    glyphSetSO = candidateSO;
                    return true;
                }
            }

            glyphSetSO = null;
            return false;
        }

        #endregion
    }
}
