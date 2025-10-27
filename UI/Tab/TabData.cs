using System;
using FakeMG.Framework.UI.Tab.TabButton;
using UnityEngine;

namespace FakeMG.Framework.UI.Tab
{
    [Serializable]
    public struct TabData : IEquatable<TabData>
    {
        [Tooltip("The button component that triggers tab selection (must implement ITabButton)")]
        [SerializeField] private TabButtonBase _tabButtonComponent;
        [SerializeField] private RectTransform _tabContent;

        public TabButtonBase TabButton => _tabButtonComponent;
        public RectTransform TabContent => _tabContent;

        public bool Equals(TabData other)
        {
            return Equals(_tabButtonComponent, other._tabButtonComponent) &&
                   Equals(_tabContent, other._tabContent);
        }

        public override bool Equals(object obj)
        {
            return obj is TabData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_tabButtonComponent, _tabContent);
        }
    }
}