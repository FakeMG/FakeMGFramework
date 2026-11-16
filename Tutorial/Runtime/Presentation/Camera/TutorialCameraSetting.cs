using System;
using UnityEngine;

namespace FakeMG.Tutorial
{
    [Serializable]
    public struct TutorialCameraSetting
    {
        [SerializeField] private float _blendDurationSeconds;

        public readonly float BlendDurationSeconds => _blendDurationSeconds;
    }
}
