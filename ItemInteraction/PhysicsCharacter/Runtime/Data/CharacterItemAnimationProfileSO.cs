using System;
using FakeMG.Framework;
using PhysicsCharacterController;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace FakeMG.ItemInteraction
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.ITEM_INTERACTION + "/Character Animation Profile")]
    public sealed class CharacterItemAnimationProfileSO : ItemAnimationProfileSO, ICharacterLocomotionOverride
    {
        [Header("Locomotion Overrides - Optional")]
        [Tooltip("Replaces the ground locomotion of each listed character state while this item is held. "
            + "Other states keep their base locomotion with Holding Idle layered over it.")]
        [SerializeField] private StateLocomotionOverride[] _locomotionOverrides = Array.Empty<StateLocomotionOverride>();

        [Tooltip("Replaces the jump and fall animations of each listed character state while this item is held.")]
        [SerializeField] private StateAirborneOverride[] _airborneOverrides = Array.Empty<StateAirborneOverride>();

        [Header("Holding Idle Replacement")]
        [Tooltip("Plays a state's locomotion override on the whole body while the item is only held, instead of layering "
            + "Holding Idle over it. Enable when the overrides already hold the item. Use Item still layers over them.")]
        [FormerlySerializedAs("_doesLocomotionReplaceHoldingIdle")]
        [SerializeField] private bool _isHoldingPoseReplacedByLocomotionOverride;

        public bool IsHoldingPoseReplacedByLocomotionOverride => _isHoldingPoseReplacedByLocomotionOverride;

        #region Public Methods

        public LocomotionAnimationDataSO GetLocomotionOverride(StateSO stateSO)
        {
            foreach (StateLocomotionOverride locomotionOverride in _locomotionOverrides)
            {
                if (locomotionOverride.StateSO == stateSO)
                {
                    return locomotionOverride.LocomotionSO;
                }
            }

            return null;
        }

        public AirborneAnimationDataSO GetAirborneOverride(StateSO stateSO)
        {
            foreach (StateAirborneOverride airborneOverride in _airborneOverrides)
            {
                if (airborneOverride.StateSO == stateSO)
                {
                    return airborneOverride.AirborneSO;
                }
            }

            return null;
        }

#if UNITY_EDITOR
        public void ConfigureLocomotionOverridesForEditor(
            bool isHoldingPoseReplacedByLocomotionOverride,
            StateLocomotionOverride[] locomotionOverrides,
            StateAirborneOverride[] airborneOverrides)
        {
            _isHoldingPoseReplacedByLocomotionOverride = isHoldingPoseReplacedByLocomotionOverride;
            _locomotionOverrides = locomotionOverrides;
            _airborneOverrides = airborneOverrides;
        }
#endif

        #endregion
    }

    [Serializable]
    public struct StateLocomotionOverride
    {
        [AssetsOnly, Required]
        [SerializeField] private StateSO _stateSO;
        [AssetsOnly, Required]
        [SerializeField] private LocomotionAnimationDataSO _locomotionSO;

        public StateSO StateSO => _stateSO;
        public LocomotionAnimationDataSO LocomotionSO => _locomotionSO;

        public StateLocomotionOverride(StateSO stateSO, LocomotionAnimationDataSO locomotionSO)
        {
            _stateSO = stateSO;
            _locomotionSO = locomotionSO;
        }
    }

    [Serializable]
    public struct StateAirborneOverride
    {
        [AssetsOnly, Required]
        [SerializeField] private StateSO _stateSO;
        [AssetsOnly, Required]
        [SerializeField] private AirborneAnimationDataSO _airborneSO;

        public StateSO StateSO => _stateSO;
        public AirborneAnimationDataSO AirborneSO => _airborneSO;

        public StateAirborneOverride(StateSO stateSO, AirborneAnimationDataSO airborneSO)
        {
            _stateSO = stateSO;
            _airborneSO = airborneSO;
        }
    }
}
