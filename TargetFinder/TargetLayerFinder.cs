using Sirenix.OdinInspector;
using UnityEngine;

namespace FakeMG.TargetFinder {
    public class TargetLayerFinder : TargetFinder {
        [Title("Target Layer Finder")]
        [SerializeField] private LayerMask targetLayerMask;
        [MinValue(1)]
        [SerializeField] private int maxTargets = 10;
        [SerializeField] private bool findClosestTarget = true;

        private Collider[] _colliders;
        private bool _isInitialized;

        private void Awake() {
            _colliders = new Collider[maxTargets];
        }

        protected override GameObject ChooseATargetInRange() {
            if (_colliders == null || _colliders.Length != maxTargets) {
                Debug.LogWarning("TargetLayerFinder: Collider array not initialized or size mismatch.");
                return null;
            }

            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, radius, _colliders, targetLayerMask);
            
            if (hitCount <= 0) return null;
            
            if (!findClosestTarget) return _colliders[0]?.gameObject;

            GameObject closestTarget = null;
            float closestDistanceSqr = float.MaxValue;
            
            for (int i = 0; i < hitCount; i++) {
                if (!_colliders[i]) continue;
                
                Vector3 directionToTarget = _colliders[i].transform.position - transform.position;
                float distanceSqr = directionToTarget.sqrMagnitude;
                
                if (distanceSqr < closestDistanceSqr) {
                    closestDistanceSqr = distanceSqr;
                    closestTarget = _colliders[i].gameObject;
                }
            }

            return closestTarget;
        }
    }
}