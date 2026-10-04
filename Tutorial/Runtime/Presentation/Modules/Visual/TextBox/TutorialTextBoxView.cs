using TMPro;
using UnityEngine;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Visual view that displays instruction text via TextMeshPro, positioned near a UI
    /// target with an offset so it does not cover the target, clamped on screen.
    /// </summary>
    public sealed class TutorialTextBoxView : AnimatedTutorialVisualView
    {
        [SerializeField] private TMP_Text _instructionText;
        [SerializeField] private RectTransform _box;
        [SerializeField] private Vector2 _targetOffsetPixels = new(0f, 180f);
        [SerializeField] private Vector2 _screenPaddingPixels = new(48f, 48f);

        public void SetInstruction(string instruction)
        {
            _instructionText.text = instruction;
        }

        public void PositionNear(RectTransform target)
        {
            Vector3 desired = target.position + (Vector3)_targetOffsetPixels;
            _box.position = ClampToScreen(desired, _screenPaddingPixels);
        }
    }
}
