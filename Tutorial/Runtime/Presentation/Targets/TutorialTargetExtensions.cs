using UnityEngine;

namespace FakeMG.Tutorial
{
    public static class TutorialTargetExtensions
    {
        /// <summary>
        /// True when the target is a Unity object that has already been destroyed. Tutorial
        /// code holds targets behind the <see cref="ITutorialTarget"/> interface, which
        /// bypasses Unity's overloaded null check, so destruction must be tested through the
        /// Object cast rather than a plain reference-null comparison.
        /// </summary>
        public static bool IsDestroyed(this ITutorialTarget target)
        {
            return target is Object unityObject && unityObject == null;
        }
    }
}
