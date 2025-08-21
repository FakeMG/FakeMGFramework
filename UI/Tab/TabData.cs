using System;
using FakeMG.FakeMGFramework.UI.Tab.TabButton;
using UnityEngine;

namespace FakeMG.FakeMGFramework.UI.Tab
{
    [Serializable]
    public struct TabData : IEquatable<TabData>
    {
        [Tooltip("The button component that triggers tab selection (must implement ITabButton)")]
        [SerializeField] private TabButtonBase tabButtonComponent;
        [SerializeField] private RectTransform tabContent;

        public TabButtonBase TabButton => tabButtonComponent;
        public RectTransform TabContent => tabContent;

        public bool Equals(TabData other)
        {
            return Equals(tabButtonComponent, other.tabButtonComponent) &&
                   Equals(tabContent, other.tabContent);
        }

        public override bool Equals(object obj)
        {
            return obj is TabData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(tabButtonComponent, tabContent);
        }
    }
}