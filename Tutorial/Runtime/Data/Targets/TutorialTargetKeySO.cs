using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Marker asset that identifies a tutorial target. Targets and the modules /
    /// conditions that reference them point at the same asset instead of matching a
    /// hand-typed id, so references are rename-safe and findable in the editor.
    /// </summary>
    [CreateAssetMenu(menuName = FakeMGEditorMenus.TUTORIAL + "/Target Key", fileName = "TutorialTargetKey")]
    public sealed class TutorialTargetKeySO : ScriptableObject
    {
    }
}
