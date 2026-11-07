using System.Collections.Generic;
using FakeMG.Framework.UI;
using UnityEngine;
using VContainer;

namespace FakeMG.InputHints
{
    /// <summary>
    /// Lists the active hints in one strip along a screen edge, as console games do, drawn for the device the player uses
    /// now. Hints that device cannot perform are left out, and the bar hides while it has nothing to show.
    /// </summary>
    public sealed class InputHintBar : MonoBehaviour
    {
        [SerializeField] private InputHintBarEntry _entryPrefab;
        [SerializeField] private Transform _entryParent;
        [Tooltip("The bar's visible frame, hidden while no hint is shown.")]
        [SerializeField] private GameObject _content;

        private readonly List<InputGlyph> _glyphs = new();
        private ReusableViewList<InputHintBarEntry> _entries;
        private ActiveInputHints _activeHints;
        private ActiveGlyphSetTracker _glyphSetTracker;
        private InputGlyphResolver _glyphResolver;
        private bool _isRedrawNeeded;

        #region Unity Lifecycle

        private void Awake()
        {
            _entries = new ReusableViewList<InputHintBarEntry>(_entryPrefab, _entryParent);
        }

        private void Start()
        {
            _activeHints.OnChanged += RequestRedraw;
            _glyphSetTracker.OnGlyphsChanged += RequestRedraw;
            Redraw();
        }

        /// <remarks>Several contexts often change in one frame, e.g. a panel closing as another opens; they redraw once.</remarks>
        private void LateUpdate()
        {
            if (_isRedrawNeeded)
            {
                Redraw();
            }
        }

        private void OnDestroy()
        {
            _activeHints.OnChanged -= RequestRedraw;
            _glyphSetTracker.OnGlyphsChanged -= RequestRedraw;
        }

        #endregion

        #region Public Methods

        [Inject]
        public void Construct(ActiveInputHints activeHints, ActiveGlyphSetTracker glyphSetTracker, InputGlyphResolver glyphResolver)
        {
            _activeHints = activeHints;
            _glyphSetTracker = glyphSetTracker;
            _glyphResolver = glyphResolver;
        }

        #endregion

        #region Private Methods

        private void RequestRedraw()
        {
            _isRedrawNeeded = true;
        }

        private void Redraw()
        {
            _isRedrawNeeded = false;
            int shownCount = 0;
            InputGlyphSetSO glyphSetSO = _glyphSetTracker.ActiveSetSO;
            if (glyphSetSO)
            {
                foreach (InputHint hint in _activeHints.Hints)
                {
                    _glyphs.Clear();
                    _glyphResolver.Resolve(hint, glyphSetSO, _glyphs);
                    if (_glyphs.Count > 0)
                    {
                        _entries.ShowAt(shownCount++).Show(hint, _glyphs);
                    }
                }
            }

            _entries.HideFrom(shownCount);
            _content.SetActive(shownCount > 0);
        }

        #endregion
    }
}
