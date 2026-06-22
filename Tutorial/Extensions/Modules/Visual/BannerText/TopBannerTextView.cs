using TMPro;
using UnityEngine;

namespace FakeMG.Tutorial
{
    public sealed class TopBannerTextView : AnimatedTutorialVisualView
    {
        [SerializeField] private TextMeshProUGUI _instructionText;

        #region Public Methods

        public void SetInstruction(string instruction)
        {
            _instructionText.text = instruction;
        }

        #endregion
    }
}
