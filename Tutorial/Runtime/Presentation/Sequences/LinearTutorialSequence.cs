using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Reusable non-branching sequence: runs its authored steps in order, resuming at
    /// the first step that saved progress has not yet completed or skipped. A forced
    /// replay (empty throwaway progress) runs every step from the start.
    /// </summary>
    public sealed class LinearTutorialSequence : MonoBehaviour, ITutorialSequence
    {
        [SerializeField] private string _tutorialId;
        [SerializeField] private List<TutorialStepBase> _steps = new();

        public TutorialId Id => new(_tutorialId);

        #region Public Methods

        public ITutorialStep GetFirstStep(TutorialProgress progress)
        {
            return FirstUnresolvedFrom(0, progress);
        }

        public ITutorialStep GetNextStep(StepId currentStepId, TutorialStepResult lastResult,
            TutorialProgress progress)
        {
            int currentIndex = IndexOf(currentStepId);
            if (currentIndex < 0)
            {
                return null;
            }

            return FirstUnresolvedFrom(currentIndex + 1, progress);
        }

        public bool IsValidEndState(StepId lastStepId, TutorialProgress progress) => true;

        #endregion

        #region Private Methods

        private ITutorialStep FirstUnresolvedFrom(int startIndex, TutorialProgress progress)
        {
            for (int i = startIndex; i < _steps.Count; i++)
            {
                StepId stepId = _steps[i].Id;
                if (!progress.IsStepCompleted(Id, stepId) && !progress.IsStepSkipped(Id, stepId))
                {
                    return _steps[i];
                }
            }

            return null;
        }

        private int IndexOf(StepId stepId)
        {
            for (int i = 0; i < _steps.Count; i++)
            {
                if (_steps[i].Id.Equals(stepId))
                {
                    return i;
                }
            }

            return -1;
        }

        #endregion
    }
}
