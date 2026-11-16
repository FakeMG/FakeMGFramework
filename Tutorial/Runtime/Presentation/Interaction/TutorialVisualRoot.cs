using UnityEngine;

namespace FakeMG.Tutorial
{
    public sealed class TutorialVisualRoot : MonoBehaviour
    {
        [SerializeField] private RectTransform _root;

        public RectTransform Root => _root;
    }
}
