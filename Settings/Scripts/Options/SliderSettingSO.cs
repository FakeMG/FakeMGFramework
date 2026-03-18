using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.Settings
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.SETTINGS_COMMON + "/Slider Setting")]
    public class SliderSettingSO : SettingDefinitionGenericSO<float>
    {
        [SerializeField] private float _storageMinValue;
        [SerializeField] private float _storageMaxValue = 1f;

        public float StorageMinValue => _storageMinValue;
        public float StorageMaxValue => _storageMaxValue;
    }
}