using Animancer;
using UnityEngine;

namespace FakeMG.ItemInteraction
{
    /// <summary>
    /// Pulls the hand that is not holding the item onto the held prefab's <see cref="HeldItemOffHandGrip"/> with humanoid IK.
    /// Items without a grip leave both hands animated. Must sit on the Animator's GameObject to receive the IK pass.
    /// </summary>
    public sealed class CharacterOffHandItemIK : MonoBehaviour, IOffHandGripPresentation
    {
        [SerializeField] private AnimancerComponent _animancer;

        [Tooltip("How long the hand takes to reach the grip after an item is shown.")]
        [SerializeField, Min(0.01f)] private float _blendDurationSeconds = 0.15f;

        private readonly OffHandGripSolver _solver = new();
        private HeldItemOffHandGrip _grip;
        private AvatarIKGoal _holdingHandGoal;
        private AvatarIKGoal _offHandGoal;
        private Vector3 _holdingHandPositionMeters;
        private Quaternion _holdingHandRotation;

        #region Unity Lifecycle

        private void Awake()
        {
            _animancer.Graph.ApplyAnimatorIK = true;
        }

        private void Update()
        {
            _solver.BlendWeight(_grip != null, Time.deltaTime, _blendDurationSeconds);
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (_grip == null)
            {
                return;
            }

            // The holding hand is never solved, so its goal is this frame's animated hand, before transforms are written.
            Animator animator = _animancer.Animator;
            _holdingHandPositionMeters = animator.GetIKPosition(_holdingHandGoal);
            _holdingHandRotation = animator.GetIKRotation(_holdingHandGoal);
            if (!_solver.HasMeasuredGrip)
            {
                return;
            }

            animator.SetIKPosition(_offHandGoal, _solver.GetOffHandPositionMeters(_holdingHandPositionMeters, _holdingHandRotation));
            animator.SetIKRotation(_offHandGoal, _solver.GetOffHandRotation(_holdingHandRotation));
            animator.SetIKPositionWeight(_offHandGoal, _solver.Weight01);
            animator.SetIKRotationWeight(_offHandGoal, _solver.Weight01 * _grip.RotationWeight01);
        }

        private void LateUpdate()
        {
            if (_grip == null)
            {
                return;
            }

            Transform gripTransform = _grip.transform;
            _solver.MeasureGrip(_holdingHandPositionMeters, _holdingHandRotation, gripTransform.position, gripTransform.rotation);
        }

        #endregion

        #region Public Methods

        public void BeginOffHandGrip(HeldItemHand holdingHand, GameObject heldItemInstance)
        {
            _grip = heldItemInstance.GetComponentInChildren<HeldItemOffHandGrip>();
            _solver.ForgetGrip();
            bool isHeldInLeftHand = holdingHand == HeldItemHand.Left;
            _holdingHandGoal = isHeldInLeftHand ? AvatarIKGoal.LeftHand : AvatarIKGoal.RightHand;
            _offHandGoal = isHeldInLeftHand ? AvatarIKGoal.RightHand : AvatarIKGoal.LeftHand;
        }

        public void ReleaseOffHandGrip()
        {
            _grip = null;
            _solver.ReleaseGrip();
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(AnimancerComponent animancer)
        {
            _animancer = animancer;
        }
#endif

        #endregion
    }
}
