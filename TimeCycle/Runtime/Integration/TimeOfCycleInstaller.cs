using System.Collections.Generic;
using FakeMG.Framework;
using FakeMG.SaveLoad;
using FakeMG.SceneLoading;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FakeMG.TimeCycle
{
    /// <summary>
    /// Registers a time-of-cycle runtime and its scene-specific output applicators in a VContainer scope.
    /// </summary>
    public static class TimeOfCycleInstaller
    {
        #region Public Methods

        public static void InstallGameplay(
            IContainerBuilder builder,
            TimeOfCycleProfileSO sharedProfileSO,
            IReadOnlyList<MonoBehaviour> outputApplicatorBehaviours)
        {
            List<ITimeOfCycleOutputApplicator> outputApplicators = CollectOutputApplicators(outputApplicatorBehaviours);
            for (int applicatorIndex = 0; applicatorIndex < outputApplicators.Count; applicatorIndex++)
            {
                builder.RegisterInstance(outputApplicators[applicatorIndex]);
            }

            builder.RegisterInstance(sharedProfileSO);
            builder.RegisterEntryPoint<TimeOfCycleService>().AsSelf().As<ITimeOfCycle>();
            builder.RegisterEntryPoint<WorldTimelineService>()
                .AsSelf()
                .As<IWorldTimeline>()
                .As<ILoadedSceneDataApplier>();
        }

        public static void InstallPersistence(IContainerBuilder builder)
        {
            builder.Register<WorldTimelinePersistence>(Lifetime.Singleton);
            builder.RegisterComponentInHierarchy<WorldTimelineSaveable>().As<ISaveable>();
        }

        #endregion

        #region Private Methods

        private static List<ITimeOfCycleOutputApplicator> CollectOutputApplicators(IReadOnlyList<MonoBehaviour> outputApplicatorBehaviours)
        {
            var outputApplicators = new List<ITimeOfCycleOutputApplicator>(outputApplicatorBehaviours.Count);
            for (int behaviourIndex = 0; behaviourIndex < outputApplicatorBehaviours.Count; behaviourIndex++)
            {
                MonoBehaviour applicatorBehaviour = outputApplicatorBehaviours[behaviourIndex];
                if (applicatorBehaviour is ITimeOfCycleOutputApplicator outputApplicator)
                {
                    outputApplicators.Add(outputApplicator);
                    continue;
                }

                Echo.Error(
                    $"Time-of-cycle applicator reference at index {behaviourIndex} does not implement " +
                    $"{nameof(ITimeOfCycleOutputApplicator)}.",
                    context: applicatorBehaviour);
            }

            return outputApplicators;
        }

        #endregion
    }
}
