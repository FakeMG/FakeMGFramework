using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.ItemInteraction
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.ITEM_INTERACTION + "/Camera Profile")]
    public sealed class ItemCameraProfileSO : ScriptableObject
    {
        [Header("Aiming")]
        [Tooltip("Vertical field of view while aiming, in degrees.")]
        [Range(1f, 179f)]
        [SerializeField] private float _aimFieldOfViewDegrees = 35f;
        [Tooltip("Camera target horizontal offset in meters.")]
        [SerializeField] private float _targetXOffsetMeters = 0.7f;

        public float AimFieldOfViewDegrees => _aimFieldOfViewDegrees;
        public float TargetXOffsetMeters => _targetXOffsetMeters;
    }
}
