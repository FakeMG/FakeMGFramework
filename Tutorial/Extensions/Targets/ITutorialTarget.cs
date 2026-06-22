using UnityEngine;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// High-level abstraction for anything a tutorial step can point at or gate
    /// interaction on, identified by a key asset. Implementations adapt UI elements,
    /// world objects, or abstract conditions.
    /// </summary>
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
