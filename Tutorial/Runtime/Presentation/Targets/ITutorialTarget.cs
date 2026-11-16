using UnityEngine;

namespace FakeMG.Tutorial
{
    public interface ITutorialTarget
    {
        TutorialTargetKeySO Key { get; }
        bool IsAvailable { get; }

        /// <summary>
        /// The transform whose hierarchy raycast hits are allowed through while this
        /// target is permitted. Null for abstract targets with no on-screen object.
        /// </summary>
        Transform InteractionTransform { get; }
    }
}
