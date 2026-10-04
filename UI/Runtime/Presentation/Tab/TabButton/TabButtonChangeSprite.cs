using UnityEngine;
using UnityEngine.UI;

namespace FakeMG.Framework.UI.Tab.TabButton
{
    public class TabButtonChangeSprite : TabButtonBase
    {
        [SerializeField] private Sprite _selectedSprite;
        [SerializeField] private Color _selectedColor = Color.white;
        [SerializeField] private Image _buttonImage;

        private Sprite _originalSprite;
        private Color _originalColor;
        private bool _originalsCached;

        public override void AnimateSelection()
        {
            InstantlySelect();
        }

        public override void AnimateDeselection()
        {
            InstantlyDeselect();
        }

        public override void InstantlySelect()
        {
            if (!_originalsCached)
            {
                _originalSprite = _buttonImage.sprite;
                _originalColor = _buttonImage.color;
                _originalsCached = true;
            }

            _buttonImage.sprite = _selectedSprite;
            _buttonImage.color = _selectedColor;
        }

        public override void InstantlyDeselect()
        {
            if (!_originalsCached) return;

            _buttonImage.sprite = _originalSprite;
            _buttonImage.color = _originalColor;
        }
    }
}
