using UnityEngine;
using UnityEngine.UI;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Visual view that dims the screen with a black background and frames a UI target to
    /// focus attention on it. The frame matches the target's screen position and size
    /// (plus padding). The background fades with the view's canvas group.
    /// </summary>
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
