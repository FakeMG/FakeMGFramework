using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FakeMG.Framework.UI.Popup
{
    internal sealed class PopupLifecycleSubscriber : IDisposable
    {
        private readonly PopupAnimator _animator;
        private readonly AssetReferenceT<GameObject> _prefabReference;
        private readonly Action<AssetReferenceT<GameObject>> _beforeShow;
        private readonly Action<AssetReferenceT<GameObject>> _afterShow;
        private readonly Action<AssetReferenceT<GameObject>> _beforeHide;
        private readonly Action<AssetReferenceT<GameObject>> _afterHide;

        public PopupLifecycleSubscriber(
            PopupAnimator animator,
            AssetReferenceT<GameObject> prefabReference,
            Action<AssetReferenceT<GameObject>> beforeShow,
            Action<AssetReferenceT<GameObject>> afterShow,
            Action<AssetReferenceT<GameObject>> beforeHide,
            Action<AssetReferenceT<GameObject>> afterHide)
        {
            _animator = animator;
            _prefabReference = prefabReference;
            _beforeShow = beforeShow;
            _afterShow = afterShow;
            _beforeHide = beforeHide;
            _afterHide = afterHide;
        }

        #region Public Methods

        public void Subscribe()
        {
            _animator.OnShowStart += RegisterShowingPopup;
            _animator.OnShowFinished += FinishShowingPopup;
            _animator.OnHideStart += BeginHidingPopup;
            _animator.OnHideFinished += RemoveHiddenPopup;
        }

        public void Dispose()
        {
            _animator.OnShowStart -= RegisterShowingPopup;
            _animator.OnShowFinished -= FinishShowingPopup;
            _animator.OnHideStart -= BeginHidingPopup;
            _animator.OnHideFinished -= RemoveHiddenPopup;
        }

        #endregion

        #region Private Methods

        private void RegisterShowingPopup() => _beforeShow(_prefabReference);
        private void FinishShowingPopup() => _afterShow(_prefabReference);
        private void BeginHidingPopup() => _beforeHide(_prefabReference);
        private void RemoveHiddenPopup() => _afterHide(_prefabReference);

        #endregion
    }
}
