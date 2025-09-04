using UnityEngine;
using UnityEngine.UI;

namespace FakeMG.Framework.UI.Tab.TabButton
{
    public class TabButtonChangeSprite : TabButtonBase
    {
        [SerializeField] private Sprite selectedSprite;
        [SerializeField] private Image buttonImage;

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
                _originalSprite = buttonImage.sprite;
            }

            buttonImage.sprite = selectedSprite;
        }

        public override void InstantlyDeselect()
        {
            buttonImage.sprite = _originalSprite;
        }
    }
}