using FakeMG.Framework.UI.Tab;
using FakeMG.InputHints;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace FakeMG.Settings
{
    /// <summary>Relinks navigation when another tab is shown, or when the active device changes which rows a page shows.</summary>
    public sealed class SettingsTabNavigationSubscriber : MonoBehaviour
    {
        [Required, SerializeField] private TabSystem _tabs;
        [Required, SerializeField] private SettingsTabNavigation _navigation;

        private ActiveGlyphSetTracker _glyphSetTracker;

        #region Unity Lifecycle

        private void OnEnable()
        {
            _tabs.OnTabChanged += _navigation.LinkShownPage;
            _glyphSetTracker.OnGlyphsChanged += _navigation.LinkCurrentPage;
        }

        private void OnDisable()
        {
            _tabs.OnTabChanged -= _navigation.LinkShownPage;
            _glyphSetTracker.OnGlyphsChanged -= _navigation.LinkCurrentPage;
        }

        #endregion

        #region Public Methods

        [Inject]
        public void Construct(ActiveGlyphSetTracker glyphSetTracker)
        {
            _glyphSetTracker = glyphSetTracker;
        }

        #endregion
    }
}
