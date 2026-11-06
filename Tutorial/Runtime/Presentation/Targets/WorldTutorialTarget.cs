using UnityEngine;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Marks a world-space point (a destination, a landmark) as a tutorial target identified by a
    /// key asset. Self-registers while enabled, like <see cref="UiTutorialTarget"/>.
    /// </summary>
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
