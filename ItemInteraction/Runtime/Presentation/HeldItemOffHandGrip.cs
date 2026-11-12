using UnityEngine;

namespace FakeMG.ItemInteraction
{
    /// <summary>
    /// Marks where the hand that is not holding the item grips it, such as the lower part of a two-handed hilt.
    /// Place it on a child of the held prefab at the wrist position of the gripping hand.
    /// </summary>
    public sealed class HeldItemOffHandGrip : MonoBehaviour
    {
        [Tooltip("How much the hand also turns to match this transform. 0 keeps the hand's animated rotation and only moves it.")]
        [SerializeField, Range(0f, 1f)] private float _rotationWeight01;

        public float RotationWeight01 => _rotationWeight01;
    }
}
