using System.Collections.Generic;
using FakeMG.Framework.UI.Tab.TabButton;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FakeMG.Framework.UI.Tab
{
    public abstract class TabBarAnimatorBase : MonoBehaviour
    {
        #region Public Methods
        public abstract void ApplyInstantState(int selectedTabIndex);

        public abstract void AnimateSelectionChange(int previousTabIndex, int selectedTabIndex);

#if UNITY_EDITOR
        [Button("Gather Child Tab Buttons")]
        public void GatherChildTabButtons()
        {
            if (!TabButtonGatherer.TryGetChildTabButtons(this, out List<TabButtonBase> tabButtons))
            {
                return;
            }

            Undo.RecordObject(this, "Gather Child Tab Buttons");
            ApplyGatheredTabButtons(tabButtons);
            EditorUtility.SetDirty(this);

            Echo.Log($"{GetType().Name} on {name} gathered {tabButtons.Count} child tab buttons in hierarchy order.", this);
        }
#endif
        #endregion

#if UNITY_EDITOR
        #region Editor Methods
        protected abstract void ApplyGatheredTabButtons(IReadOnlyList<TabButtonBase> tabButtons);
        #endregion
#endif
    }
}
