using Sirenix.OdinInspector;
using UnityEngine;

namespace FakeMG.Framework.TargetFinder
{
    public class TargetLayerFinder : TargetFinder
    {
        [Title("Target Layer Finder")]
        [SerializeField] private LayerMask _targetLayerMask;
        [MinValue(1)]
        [SerializeField] private int _maxTargets = 10;
        [SerializeField] private bool _findClosestTarget = true;

        private Collider[] _colliders;

        private void Awake()
        {
            _colliders = new Collider[_maxTargets];
        }

        protected override GameObject ChooseATargetInRange()
        {
            if (_colliders == null || _colliders.Length != _maxTargets)
            {
                Debug.LogWarning("TargetLayerFinder: Collider array not initialized or size mismatch.");
                return null;
            }

            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, _radius, _colliders, _targetLayerMask);

            if (hitCount <= 0) return null;

            if (!_findClosestTarget) return _colliders[0]?.gameObject;

            GameObject closestTarget = null;
            float closestDistanceSqr = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                if (!_colliders[i]) continue;

                Vector3 directionToTarget = _colliders[i].transform.position - transform.position;
                float distanceSqr = directionToTarget.sqrMagnitude;

                if (distanceSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distanceSqr;
                    closestTarget = _colliders[i].gameObject;
                }
            }

            return closestTarget;
        }
    }
}