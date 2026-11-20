using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace FakeMG.Framework.UI
{
    /// <summary>
    /// Lays out a list whose children never change once, then disables its layout components so later canvas rebuilds skip
    /// them.
    /// </summary>
    /// <remarks>Only for static content: a child added, removed or resized afterwards is not laid out again.</remarks>
    public sealed class OneTimeLayout : MonoBehaviour
    {
        [Required, SerializeField] private RectTransform _layoutRoot;
        [Required, SerializeField] private Behaviour[] _layoutComponents;

        #region Unity Lifecycle

        private void Start()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_layoutRoot);
            foreach (Behaviour layoutComponent in _layoutComponents)
            {
                layoutComponent.enabled = false;
            }
        }

        #endregion
    }
}
