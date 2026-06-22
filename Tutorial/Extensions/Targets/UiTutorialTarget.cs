using UnityEngine;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Adapts a UI element into a tutorial target identified by a key asset. It
    /// self-registers with the target registry while enabled and unregisters when
    /// disabled, so it works whether it lives in the scene or is instantiated at runtime
    /// (e.g. a button inside a popup).
    /// </summary>
    public sealed class UiTutorialTarget : SelfRegisteringTutorialTarget
    {
        [SerializeField] private TutorialTargetKeySO _key;

        public override TutorialTargetKeySO Key => _key;

        public override bool IsAvailable => isActiveAndEnabled && gameObject.activeInHierarchy;

        public override Transform InteractionTransform => transform;

        #region Unity Lifecycle

        private void OnEnable()
        {
            Register();
        }

        #endregion
    }
}
