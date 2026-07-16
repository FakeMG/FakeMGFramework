using System.Collections.Generic;
using VContainer;
using VContainer.Unity;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Registers a time-of-cycle runtime and its scene-specific output applicators in a VContainer scope.
    /// </summary>
    public static class TimeOfCycleInstaller
    {
        #region Public Methods

        public static void Register(
            IContainerBuilder builder,
            TimeOfCycleProfileSO sharedProfileSO,
            IReadOnlyList<ITimeOfCycleOutputApplicator> outputApplicators)
        {
            builder.RegisterInstance(sharedProfileSO);
            for (int applicatorIndex = 0; applicatorIndex < outputApplicators.Count; applicatorIndex++)
            {
                builder.RegisterInstance(outputApplicators[applicatorIndex]);
            }

            builder.RegisterEntryPoint<TimeOfCycleService>()
                .AsSelf()
                .As<ITimeOfCycle>();
        }

        #endregion
    }
}
