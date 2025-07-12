using UnityEngine;
using UnityEngine.UI;

namespace FakeMG.FakeMGFramework.UI.Tab.TabButton
{
    public abstract class TabButtonBase : MonoBehaviour
    {
        public Button button;
        
        public abstract void AnimateSelection();
        public abstract void AnimateDeselection();
        public abstract void InstantlySelect();
        public abstract void InstantlyDeselect();
    }
}