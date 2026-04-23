using System;
using UnityEngine;
using UnityEngine.UI;

namespace FakeMG.Framework.UI.Tab.TabButton
{
    public abstract class TabButtonBase : MonoBehaviour
    {
        [SerializeField] private Button _button;

        public event Action<TabButtonBase> SelectionRequested;

        public Button Button => _button;

        #region Unity Lifecycle
        protected virtual void Reset()
        {
            _button = GetComponent<Button>();
        }

        protected virtual void OnEnable()
        {
            _button.onClick.AddListener(RequestSelection);
        }

        protected virtual void OnDisable()
        {
            _button.onClick.RemoveListener(RequestSelection);
        }
        #endregion

        #region Public Methods
        public abstract void AnimateSelection();
        public abstract void AnimateDeselection();
        public abstract void InstantlySelect();
        public abstract void InstantlyDeselect();
        #endregion

        #region Private Methods
        private void RequestSelection()
        {
            SelectionRequested?.Invoke(this);
        }
        #endregion
    }
}
