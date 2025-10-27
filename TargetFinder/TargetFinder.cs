using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FakeMG.Framework.TargetFinder
{
    public abstract class TargetFinder : MonoBehaviour
    {
        [Title("Target Finder")]
        [SerializeField] protected float _radius = 10f;
        [SerializeField] private float _targetDetectionInterval = 1f;

        [Title("Line of Sight")]
        [SerializeField] private bool _requireLineOfSight;
        [ShowIf("requireLineOfSight")]
        [SerializeField] private bool _is2DMode;
        [ShowIf("requireLineOfSight")]
        [SerializeField] private LayerMask _obstacleLayerMask;

        [Title("Debug")]
        [SerializeField] private bool _showGizmos = true;
        [SerializeField] private Color _radiusColor = Color.red;
        [SerializeField] private Color _lineColor = Color.green;

        private WaitForSeconds _waitForSeconds;
        private Coroutine _findTargetCoroutine;
        private bool _isActive;

        public Action<GameObject> OnNewTargetFound;
        public Action<GameObject> OnTargetInRange;
        public Action OnTargetLost;

        public GameObject Target { get; private set; }

        private bool _isTargetInRange;

        protected virtual void OnEnable()
        {
            _isActive = true;
            StartTargetFinding();
        }

        private void Start()
        {
            _waitForSeconds = new WaitForSeconds(_targetDetectionInterval);

            StartCoroutine(FindTarget());
        }

        protected virtual void OnDisable()
        {
            _isActive = false;
            StopTargetFinding();
        }

        private void StartTargetFinding()
        {
            StopTargetFinding();
            _findTargetCoroutine = StartCoroutine(FindTarget());
        }

        private void StopTargetFinding()
        {
            if (_findTargetCoroutine != null)
            {
                StopCoroutine(_findTargetCoroutine);
                _findTargetCoroutine = null;
            }
        }

        public void SetRadius(float newRadius)
        {
            _radius = newRadius;
        }

        private IEnumerator FindTarget()
        {
            while (_isActive)
            {
                GameObject selectedTarget = ChooseATargetInRange();

                if (_requireLineOfSight && selectedTarget)
                {
                    if (!HasLineOfSight(selectedTarget))
                    {
                        selectedTarget = null;
                    }
                }

                if (selectedTarget && selectedTarget != Target)
                {
                    Target = selectedTarget;

                    if (_isActive)
                    {
                        OnNewTargetFound?.Invoke(selectedTarget);
                    }
                }

                if (!selectedTarget && _isTargetInRange)
                {
                    Target = null;
                    _isTargetInRange = false;
                    if (_isActive)
                    {
                        OnTargetLost?.Invoke();
                    }
                }

                if (Target && _isActive)
                {
                    _isTargetInRange = true;
                    OnTargetInRange?.Invoke(Target);
                }

                yield return _waitForSeconds;
            }
        }

        protected abstract GameObject ChooseATargetInRange();

        private bool HasLineOfSight(GameObject target)
        {
            Vector3 direction = target.transform.position - transform.position;

            float distance = direction.magnitude;

            // Check if positions are too close to get a meaningful direction
            if (distance < 0.5f) return true;

            return _is2DMode
                ? Check2DLineOfSight(direction.normalized, distance)
                : Check3DLineOfSight(direction.normalized, distance);
        }

        private bool Check2DLineOfSight(Vector2 direction, float distance)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction.normalized, distance, _obstacleLayerMask);
            return !hit.collider;
        }

        private bool Check3DLineOfSight(Vector3 direction, float distance)
        {
            bool hasHit = Physics.Raycast(transform.position, direction.normalized, out RaycastHit hit, distance,
                _obstacleLayerMask);
            return !hasHit;
        }

        private void OnDrawGizmos()
        {
            if (!_showGizmos) return;

            Gizmos.color = _radiusColor;
            Gizmos.DrawWireSphere(transform.position, _radius);

            if (Target)
            {
                Gizmos.color = _lineColor;
                Gizmos.DrawLine(transform.position, Target.transform.position);
            }
        }
    }
}