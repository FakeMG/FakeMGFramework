using UnityEngine;

namespace FakeMG.Settings
{
    [CreateAssetMenu(menuName = "Settings/Slider Setting")]
    public class SliderSettingSO : SettingDataGeneric<float>
    {
        [SerializeField] private float _storageMinValue;
        [SerializeField] private float _storageMaxValue = 1f;

        public float StorageMinValue => _storageMinValue;
        public float StorageMaxValue => _storageMaxValue;
    }
}