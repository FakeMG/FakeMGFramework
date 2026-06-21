using System.Collections.Generic;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.GodCamera
{
    /// <summary>
    /// Detects objects obstructing the camera-to-focus path and drives their fade transitions.
    /// </summary>
    public class CameraObstructionFader : MonoBehaviour
    {
        [SerializeField] private CameraRigView _cameraRigView;
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private LayerMask _obstructionLayerMask;
        [SerializeField] private float _obstructionRadiusMeters = 0.35f;
        [SerializeField] private float _closeDistanceMeters = 2f;
        [SerializeField] private float _fadedAlpha01 = 0.35f;
        [SerializeField] private float _fadeDurationSeconds = 0.15f;
        [SerializeField] private int _maxObstructionHits = 32;

        private readonly HashSet<FadeableObject> _activeFadeableObjects = new();
        private readonly List<FadeableObject> _trackedFadeableObjects = new();
        private RaycastHit[] _raycastHits;
        private Collider[] _closeColliders;

        #region Unity Lifecycle

        private void Awake()
        {
            _raycastHits = new RaycastHit[_maxObstructionHits];
            _closeColliders = new Collider[_maxObstructionHits];
        }

        private void Update()
        {
            DetectActiveFadeableObjects();
            UpdateFadeEffects();
        }

        private void OnDisable()
        {
            for (int i = 0; i < _trackedFadeableObjects.Count; i++)
            {
                if (_trackedFadeableObjects[i])
                {
                    _trackedFadeableObjects[i].Release();
                }
            }

            _trackedFadeableObjects.Clear();
        }

        #endregion

        #region Private Methods

        private void DetectActiveFadeableObjects()
        {
            _activeFadeableObjects.Clear();

            Vector3 cameraPositionMeters = _cameraTransform.position;
            Vector3 focusPositionMeters = _cameraRigView.FocusPositionMeters;
            Vector3 obstructionDirection = focusPositionMeters - cameraPositionMeters;
            float obstructionDistanceMeters = obstructionDirection.magnitude;

            if (obstructionDistanceMeters > 0f)
            {
                // The sphere cast approximates the camera's visual corridor instead of testing one infinitesimal ray.
                int raycastHitCount = Physics.SphereCastNonAlloc(
                    cameraPositionMeters,
                    _obstructionRadiusMeters,
                    obstructionDirection.normalized,
                    _raycastHits,
                    obstructionDistanceMeters,
                    _obstructionLayerMask);

                for (int i = 0; i < raycastHitCount; i++)
                {
                    AddFadeableObjectFrom(_raycastHits[i].collider);
                }
            }

            int closeHitCount = Physics.OverlapSphereNonAlloc(
                cameraPositionMeters,
                _closeDistanceMeters,
                _closeColliders,
                _obstructionLayerMask);

            for (int i = 0; i < closeHitCount; i++)
            {
                AddFadeableObjectFrom(_closeColliders[i]);
            }
        }

        private void AddFadeableObjectFrom(Collider targetCollider)
        {
            FadeableObject fadeableObject = targetCollider.GetComponentInParent<FadeableObject>();
            if (!fadeableObject)
            {
                return;
            }

            _activeFadeableObjects.Add(fadeableObject);
        }

        private void UpdateFadeEffects()
        {
            float deltaTimeSeconds = Time.deltaTime;

            foreach (FadeableObject fadeableObject in _activeFadeableObjects)
            {
                if (!fadeableObject)
                {
                    continue;
                }

                if (_trackedFadeableObjects.Contains(fadeableObject))
                {
                    continue;
                }

                _trackedFadeableObjects.Add(fadeableObject);
            }

            for (int i = _trackedFadeableObjects.Count - 1; i >= 0; i--)
            {
                FadeableObject fadeableObject = _trackedFadeableObjects[i];
                if (!fadeableObject)
                {
                    Echo.Warning("A fadeable object was destroyed while its fade effect was active.", context: this);
                    _trackedFadeableObjects.RemoveAt(i);
                    continue;
                }

                float targetAlpha01 = _activeFadeableObjects.Contains(fadeableObject) ? _fadedAlpha01 : 1f;
                bool isFadeEffectActive = fadeableObject.UpdateFade(
                    targetAlpha01,
                    _fadeDurationSeconds,
                    deltaTimeSeconds);

                if (!isFadeEffectActive && !_activeFadeableObjects.Contains(fadeableObject))
                {
                    fadeableObject.Release();
                    _trackedFadeableObjects.RemoveAt(i);
                }
            }
        }

        #endregion
    }
}
