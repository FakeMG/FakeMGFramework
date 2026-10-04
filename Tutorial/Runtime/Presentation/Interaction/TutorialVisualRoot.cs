using UnityEngine;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Scene marker for the UI transform that instantiated tutorial visuals are
    /// parented under. Registered in the gameplay scope so the runner can find it.
    /// </summary>
    public sealed class TutorialVisualRoot : MonoBehaviour
    {
        [SerializeField] private RectTransform _root;

        public RectTransform Root => _root;
    }
}
