using UnityEngine;
using UnityEngine.UI;

namespace FakeMG.Framework.UI.Toggle
{
    public sealed class ToggleSwitchSpriteVisual : MonoBehaviour, IToggleSwitchVisual
    {
        [SerializeField] private Image _handleImage;
        [SerializeField] private Sprite _offSprite;
        [SerializeField] private Sprite _onSprite;

        public void Apply(bool isOn, float normalizedValue, bool instant)
        {
            if (!_handleImage)
                return;

            _handleImage.sprite = isOn ? _onSprite : _offSprite;
        }
    }
}