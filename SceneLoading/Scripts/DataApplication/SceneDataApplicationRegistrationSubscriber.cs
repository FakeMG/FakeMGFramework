using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace FakeMG.SceneLoading
{
    public sealed class SceneDataApplicationRegistrationSubscriber : MonoBehaviour
    {
        [Required, SerializeField] private MonoBehaviour[] _dataApplierBehaviours;

        private ISceneDataApplicationCoordinator _coordinator;
        private readonly List<IDisposable> _registrations = new();

        #region Unity Lifecycle

        private void OnEnable()
        {
            RegisterDataAppliersIfReady();
        }

        private void OnDisable()
        {
            foreach (IDisposable registration in _registrations)
            {
                registration.Dispose();
            }

            _registrations.Clear();
        }

        #endregion

        #region Public Methods

        [Inject]
        public void Construct(ISceneDataApplicationCoordinator coordinator)
        {
            _coordinator = coordinator;
            RegisterDataAppliersIfReady();
        }

        #endregion

        #region Private Methods

        private void RegisterDataAppliersIfReady()
        {
            if (_coordinator == null || !isActiveAndEnabled || _registrations.Count > 0)
            {
                return;
            }

            foreach (MonoBehaviour dataApplierBehaviour in _dataApplierBehaviours)
            {
                if (dataApplierBehaviour is not ILoadedSceneDataApplier dataApplier)
                {
                    throw new InvalidOperationException($"'{dataApplierBehaviour.name}' does not implement {nameof(ILoadedSceneDataApplier)}.");
                }

                _registrations.Add(_coordinator.Register(gameObject.scene, dataApplier));
            }
        }

        #endregion
    }
}
