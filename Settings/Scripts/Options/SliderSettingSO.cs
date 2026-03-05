using UnityEngine;

namespace FakeMG.Settings
{
    [CreateAssetMenu(menuName = "Settings/Slider Setting")]
    public class SliderSettingSO : SettingDataGeneric<float>
    {
        [SerializeField] private float _minValue;
        [SerializeField] private float _maxValue;
        [SerializeField] private bool _useWholeNumbers;

        public float MinValue => _minValue;
        public float MaxValue => _maxValue;
        public bool UseWholeNumbers => _useWholeNumbers;
    }
}