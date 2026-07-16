using System.Collections.Generic;
using FakeMG.Framework;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Supplies serialized profile and applicator references to a self-contained time-of-cycle container.
    /// </summary>
    public sealed class TimeOfCycleLifetimeScope : LifetimeScope
    {
        [SerializeField] private TimeOfCycleProfileSO _sharedProfileSO;
        [SerializeField] private List<MonoBehaviour> _outputApplicatorBehaviours = new();

        #region Public Methods

#if UNITY_EDITOR
        public void ConfigureForEditor(
            TimeOfCycleProfileSO sharedProfileSO,
            IEnumerable<MonoBehaviour> outputApplicatorBehaviours)
        {
            _sharedProfileSO = sharedProfileSO;
            _outputApplicatorBehaviours = new List<MonoBehaviour>(outputApplicatorBehaviours);
        }
#endif

        #endregion

        #region Private Methods

        protected override void Configure(IContainerBuilder builder)
        {
            List<ITimeOfCycleOutputApplicator> outputApplicators = new(
                _outputApplicatorBehaviours.Count);
            for (int behaviourIndex = 0;
                 behaviourIndex < _outputApplicatorBehaviours.Count;
                 behaviourIndex++)
            {
                MonoBehaviour applicatorBehaviour = _outputApplicatorBehaviours[behaviourIndex];
                if (applicatorBehaviour is ITimeOfCycleOutputApplicator outputApplicator)
                {
                    outputApplicators.Add(outputApplicator);
                    continue;
                }

                Echo.Error(
                    $"Time-of-cycle applicator reference at index {behaviourIndex} does not implement " +
                    $"{nameof(ITimeOfCycleOutputApplicator)}.",
                    context: this);
            }

            TimeOfCycleInstaller.Register(builder, _sharedProfileSO, outputApplicators);
        }

        #endregion
    }
}
