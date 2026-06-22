using System;
using UnityEngine;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Serializable description of a camera move a step requests. Interpreted by the
    /// game's concrete <see cref="ITutorialCameraController"/> implementation.
    /// </summary>
    [Serializable]
    public struct TutorialCameraSetting
    {
        [SerializeField] private float _blendDurationSeconds;

        public readonly float BlendDurationSeconds => _blendDurationSeconds;
    }
}
