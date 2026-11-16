using UnityEngine;
using UnityEngine.UI;

namespace FakeMG.Tutorial
{
    public sealed class TutorialFramingVisualView : AnimatedTutorialVisualView
    {
        [SerializeField] private Image _background;
        [SerializeField] private RectTransform _frame;
        [SerializeField] private Vector2 _framePaddingPixels = new(16f, 16f);

        public void Frame(RectTransform target)
        {
            _frame.position = target.position;
            _frame.sizeDelta = target.rect.size + _framePaddingPixels;
        }
    }
}
