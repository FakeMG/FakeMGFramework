using FakeMG.Framework.EventBus;
using UnityEngine;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Base for tutorial targets that announce themselves to the registry over the event
    /// bus. Owns the register/unregister handshake (idempotent, guarded by enable state,
    /// always unregistering when disabled) so each concrete target only decides *when* it
    /// is eligible to be a target rather than re-implementing the registration protocol.
    /// </summary>
    public abstract class SelfRegisteringTutorialTarget : MonoBehaviour, ITutorialTarget
    {
        public abstract TutorialTargetKeySO Key { get; }
        public abstract bool IsAvailable { get; }
        public abstract Transform InteractionTransform { get; }

        protected bool IsRegistered { get; private set; }

        #region Unity Lifecycle

        protected virtual void OnDisable()
        {
            Unregister();
        }

        #endregion

        #region Protected Methods

        protected void Register()
        {
            if (IsRegistered || !isActiveAndEnabled)
            {
                return;
            }

            IsRegistered = true;
            EventBus<TutorialTargetRegisteredEvent>.Raise(new TutorialTargetRegisteredEvent { Target = this });
        }

        protected void Unregister()
        {
            if (!IsRegistered)
            {
                return;
            }

            IsRegistered = false;
            EventBus<TutorialTargetUnregisteredEvent>.Raise(new TutorialTargetUnregisteredEvent { Target = this });
        }

        #endregion
    }
}
