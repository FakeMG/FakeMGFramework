using System.Collections.Generic;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.ItemInteraction
{
    /// <summary>
    /// What a character holds when its hand is empty, so a selection is never null.
    /// It has no held prefab and no holding pose; its actions play through the same use pipeline as held items.
    /// Games subclass it, or implement IActionItem on their own asset, to attach data such as an empty-hand attack.
    /// </summary>
    [CreateAssetMenu(menuName = FakeMGEditorMenus.ITEM_INTERACTION + "/Empty Hand Item")]
    public class EmptyHandItemSO : ScriptableObject, IActionItem
    {
        [Header("Actions")]
        [Tooltip("What the empty hand can do. The first action is the primary one. Leave empty for a hand that does nothing.")]
        [SerializeField] private ItemActionSO[] _actionSOs = new ItemActionSO[0];

        public string DisplayName => name;
        public IReadOnlyList<ItemActionSO> Actions => _actionSOs;
        public ItemActionSO PrimaryActionSO => _actionSOs.Length > 0 ? _actionSOs[0] : null;

        #region Public Methods

#if UNITY_EDITOR
        public void ConfigureForEditor(params ItemActionSO[] actionSOs)
        {
            _actionSOs = actionSOs;
        }
#endif

        #endregion
    }
}
