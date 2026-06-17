using UnityEngine;

namespace FakeMG.Framework.UI.Toggle
{
    public sealed class ToggleSwitchGameObjectVisual : MonoBehaviour, IToggleSwitchVisual
    {
        [SerializeField] private GameObject _offObject;
        [SerializeField] private GameObject _onObject;

        public void Apply(bool isOn, float normalizedValue, bool instant)
        {
            _offObject.SetActive(!isOn);
            _onObject.SetActive(isOn);
        }
    }
}