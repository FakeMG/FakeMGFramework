using UnityEngine;
using UnityEngine.UI;

namespace FakeMG.Framework.UI.Tab.TabButton
{
    public abstract class TabButtonBase : MonoBehaviour
    {
        public Button Button;

        public abstract void AnimateSelection();
        public abstract void AnimateDeselection();
        public abstract void InstantlySelect();
        public abstract void InstantlyDeselect();
    }
}