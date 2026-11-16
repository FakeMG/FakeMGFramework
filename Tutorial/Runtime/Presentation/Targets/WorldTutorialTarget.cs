using UnityEngine;

namespace FakeMG.Tutorial
{
    public sealed class WorldTutorialTarget : SelfRegisteringTutorialTarget
    {
        [SerializeField] private TutorialTargetKeySO _key;

        public override TutorialTargetKeySO Key => _key;
        public override bool IsAvailable => isActiveAndEnabled;
        public override Transform InteractionTransform => transform;

        #region Unity Lifecycle

        private void OnEnable()
        {
            Register();
        }

        #endregion
    }
}
