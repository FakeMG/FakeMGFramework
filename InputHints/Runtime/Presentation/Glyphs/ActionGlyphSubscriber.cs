using UnityEngine;
using VContainer;

namespace FakeMG.InputHints
{
    /// <summary>Shows the action's glyph from the active device's set, again whenever the device or its bindings change.</summary>
    [RequireComponent(typeof(ActionGlyphView))]
    public sealed class ActionGlyphSubscriber : MonoBehaviour
    {
        [SerializeField] private ActionGlyphView _view;

        private ActiveGlyphSetTracker _glyphSetTracker;

        #region Unity Lifecycle

        private void OnEnable()
        {
            _glyphSetTracker.OnGlyphsChanged += ShowGlyphOfActiveDevice;
            ShowGlyphOfActiveDevice();
        }

        private void OnDisable()
        {
            _glyphSetTracker.OnGlyphsChanged -= ShowGlyphOfActiveDevice;
        }

        #endregion

        #region Public Methods

        [Inject]
        public void Construct(ActiveGlyphSetTracker glyphSetTracker)
        {
            _glyphSetTracker = glyphSetTracker;
        }

        #endregion

        #region Private Methods

        private void ShowGlyphOfActiveDevice()
        {
            _view.ShowGlyphFrom(_glyphSetTracker.ActiveSetSO);
        }

        #endregion
    }
}
