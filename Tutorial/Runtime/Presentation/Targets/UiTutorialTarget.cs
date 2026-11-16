using UnityEngine;

namespace FakeMG.Tutorial
{
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
