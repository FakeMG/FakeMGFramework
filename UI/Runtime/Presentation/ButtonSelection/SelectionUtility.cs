using UnityEngine;
using UnityEngine.UI;

namespace FakeMG.Framework.UI.ButtonSelection
{
    public static class SelectionUtility
    {
        /// <summary>
        /// The object exists and is active in the hierarchy.
        /// The EventSystem keeps its selection on deactivated objects, so this is how a lost selection is detected.
        /// </summary>
        public static bool IsAlive(GameObject target)
        {
            return target && target.activeInHierarchy;
        }

        /// <summary>
        /// The object is alive and, if it is a Selectable, enabled and interactable.
        /// Use this to decide whether something is safe to select.
        /// </summary>
        public static bool IsValidTarget(GameObject target)
        {
            if (!IsAlive(target))
            {
                return false;
            }

            if (target.TryGetComponent(out Selectable selectable))
            {
                return selectable.isActiveAndEnabled && selectable.IsInteractable();
            }

            return true;
        }
    }
}
