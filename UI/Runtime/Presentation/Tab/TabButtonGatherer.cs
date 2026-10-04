using System.Collections.Generic;
using FakeMG.Framework.UI.Tab.TabButton;
using UnityEngine;

namespace FakeMG.Framework.UI.Tab
{
    public static class TabButtonGatherer
    {
        #region Public Methods
        public static bool TryGetChildTabButtons<TButton>(Component owner, out List<TButton> tabButtons)
            where TButton : Component
        {
            tabButtons = new List<TButton>(owner.GetComponentsInChildren<TButton>(true));
            SortByHierarchyOrder(tabButtons, owner.transform);

            if (tabButtons.Count > 0)
            {
                return true;
            }

            Echo.Warning($"{owner.GetType().Name} on {owner.name} found no child tab buttons to gather.", owner);
            return false;
        }

        public static List<TButton> CastButtons<TButton>(IReadOnlyList<TabButtonBase> tabButtons, Component owner)
            where TButton : TabButtonBase
        {
            List<TButton> castButtons = new(tabButtons.Count);

            for (int i = 0; i < tabButtons.Count; i++)
            {
                if (tabButtons[i] is TButton castButton)
                {
                    castButtons.Add(castButton);
                    continue;
                }

                Echo.Error($"{owner.GetType().Name} on {owner.name} expected {typeof(TButton).Name} at gathered index {i}, but found {tabButtons[i].GetType().Name}.", owner);
            }

            return castButtons;
        }

        public static List<TabData> BuildTabsPreservingContent(IReadOnlyList<TabButtonBase> tabButtons, IReadOnlyList<TabData> existingTabs)
        {
            List<TabData> gatheredTabs = new(tabButtons.Count);

            for (int i = 0; i < tabButtons.Count; i++)
            {
                RectTransform existingTabContent = i < existingTabs.Count ? existingTabs[i].TabContent : null;
                gatheredTabs.Add(new TabData(tabButtons[i], existingTabContent));
            }

            return gatheredTabs;
        }
        #endregion

        #region Private Methods
        private static void SortByHierarchyOrder<TComponent>(List<TComponent> components, Transform root)
            where TComponent : Component
        {
            components.Sort((left, right) => CompareHierarchyOrder(left.transform, right.transform, root));
        }

        private static int CompareHierarchyOrder(Transform left, Transform right, Transform root)
        {
            List<int> leftPath = BuildSiblingIndexPath(left, root);
            List<int> rightPath = BuildSiblingIndexPath(right, root);
            int sharedDepth = Mathf.Min(leftPath.Count, rightPath.Count);

            for (int i = 0; i < sharedDepth; i++)
            {
                int comparison = leftPath[i].CompareTo(rightPath[i]);
                if (comparison != 0)
                {
                    return comparison;
                }
            }

            return leftPath.Count.CompareTo(rightPath.Count);
        }

        private static List<int> BuildSiblingIndexPath(Transform target, Transform root)
        {
            List<int> path = new();
            Transform current = target;

            while (current != null && current != root)
            {
                path.Add(current.GetSiblingIndex());
                current = current.parent;
            }

            path.Reverse();
            return path;
        }
        #endregion
    }
}
