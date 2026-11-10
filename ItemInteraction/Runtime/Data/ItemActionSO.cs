using System;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.ItemInteraction
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.ITEM_INTERACTION + "/Item Action")]
    public sealed class ItemActionSO : ScriptableObject
    {
        [Header("Behavior")]
        [Tooltip("Controls hold, release, cancellation, and repeated use.")]
        [SerializeField] private ItemInputLifecycleSO _inputLifecycleSO;
        [Tooltip("Resolves the item target from pointer and screen coordinates.")]
        [SerializeField] private ItemTargetingProfileSO _targetingProfileSO;
        [Header("Presentation")]
        [Tooltip("Holding/use poses and the moment when the action takes effect.")]
        [SerializeField] private ItemAnimationProfileSO _animationProfileSO;
        [Tooltip("Optional aiming settings, applied by the host camera adapter.")]
        [SerializeField] private ItemCameraProfileSO _cameraProfileSO;

        public ItemInputLifecycleSO InputLifecycleSO => _inputLifecycleSO;
        public ItemTargetingProfileSO TargetingProfileSO => _targetingProfileSO;
        public ItemAnimationProfileSO AnimationProfileSO => _animationProfileSO;
        public ItemCameraProfileSO CameraProfileSO => _cameraProfileSO;

        #region Public Methods

#if UNITY_EDITOR
        public void ConfigureForEditor(
            ItemInputLifecycleSO inputLifecycleSO,
            ItemTargetingProfileSO targetingProfileSO,
            ItemAnimationProfileSO animationProfileSO,
            ItemCameraProfileSO cameraProfileSO)
        {
            _inputLifecycleSO = inputLifecycleSO;
            _targetingProfileSO = targetingProfileSO;
            _animationProfileSO = animationProfileSO;
            _cameraProfileSO = cameraProfileSO;
        }
#endif

        public void ValidateOrThrow()
        {
            if (_inputLifecycleSO == null || _targetingProfileSO == null || _animationProfileSO == null)
            {
                throw new InvalidOperationException($"{name} requires input, targeting, and animation profiles.");
            }

            _animationProfileSO.ValidateOrThrow();
        }

        #endregion
    }
}
