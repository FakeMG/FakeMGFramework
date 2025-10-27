using UnityEngine;
using UnityEngine.UI;

namespace FakeMG.Framework.UI.Tab.TabButton
{
    public class TabButtonChangeSprite : TabButtonBase
    {
        [SerializeField] private Sprite _selectedSprite;
        [SerializeField] private Image _buttonImage;

        private Sprite _originalSprite;

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
            if (!_originalSprite)
            {
                _originalSprite = _buttonImage.sprite;
            }

            _buttonImage.sprite = _selectedSprite;
        }

        public override void InstantlyDeselect()
        {
            _buttonImage.sprite = _originalSprite;
        }
    }
}