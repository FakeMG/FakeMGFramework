using UnityEngine;

namespace FakeMG.Tutorial
{
    public sealed class TutorialHiddenUIRoot : MonoBehaviour
    {
        public RectTransform Root => transform as RectTransform;
    }
}
