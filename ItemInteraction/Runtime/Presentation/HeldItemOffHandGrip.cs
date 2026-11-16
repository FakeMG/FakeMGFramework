using UnityEngine;

namespace FakeMG.ItemInteraction
{
    /// <summary>
    /// Marks where the hand that is not holding the item grips it, such as the lower part of a two-handed hilt.
    /// Place it on a child of the held prefab at the wrist position of the gripping hand. Its rotation is a humanoid IK goal
    /// rotation, which is offset from the hand bone's own rotation, so copying a posed hand bone's rotation turns the hand.
    /// The grip is held fully unless something on the held prefab loosens it, such as a bow string only held while drawing.
    /// </summary>
    public sealed class HeldItemOffHandGrip : MonoBehaviour
    {
        [Tooltip("How much the hand also turns to match this transform. 0 keeps the hand's animated rotation and only moves it.")]
        [SerializeField, Range(0f, 1f)] private float _rotationWeight01;

        public float RotationWeight01 => _rotationWeight01;
        public float GripWeight01 { get; private set; } = 1f;

        #region Public Methods

        public void SetGripWeight(float gripWeight01)
        {
            GripWeight01 = Mathf.Clamp01(gripWeight01);
        }

        #endregion
    }
}
