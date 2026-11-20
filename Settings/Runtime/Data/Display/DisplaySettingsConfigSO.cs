using FakeMG.Framework;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FakeMG.Settings.Display
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.SETTINGS_DISPLAY + "/Display Settings Config SO")]
    public sealed class DisplaySettingsConfigSO : ScriptableObject
    {
        [Header("Screen")]
        [Required, SerializeField] private WindowModeOptionSettingSO _windowModeSettingSO;
        [Required, SerializeField] private ResolutionOptionSettingSO _resolutionSettingSO;

        [Space]
        [Header("Frame Pacing")]
        [Required, SerializeField] private BoolSettingSO _vSyncSettingSO;
        [Tooltip("Only takes effect while VSync is off.")]
        [Required, SerializeField] private FrameRateOptionSettingSO _frameRateSettingSO;

        public WindowModeOptionSettingSO WindowModeSettingSO => _windowModeSettingSO;
        public ResolutionOptionSettingSO ResolutionSettingSO => _resolutionSettingSO;
        public BoolSettingSO VSyncSettingSO => _vSyncSettingSO;
        public FrameRateOptionSettingSO FrameRateSettingSO => _frameRateSettingSO;
    }
}
