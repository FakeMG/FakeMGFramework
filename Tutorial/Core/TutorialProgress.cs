using System;
using System.Collections.Generic;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Serializable record of tutorial progress identified entirely by IDs. Acts as
    /// both the runtime model (read + write) and the save payload. Collections are
    /// public so every save backend (JsonUtility / ES3 / PlayerPrefs) can serialize
    /// them; mutation still goes through the methods below.
    /// </summary>
    [Serializable]
    public sealed class TutorialProgress
    {
        public List<string> CompletedTutorialIds = new();
        public List<CompletedStepRecord> CompletedSteps = new();
        public List<SkippedStepRecord> SkippedSteps = new();
        public List<BranchChoiceRecord> BranchChoices = new();

        #region Reads

        public bool IsTutorialCompleted(TutorialId tutorialId) =>
            CompletedTutorialIds.Contains(tutorialId.Value);

        public bool IsStepCompleted(TutorialId tutorialId, StepId stepId) =>
            FindStepIndex(CompletedSteps, tutorialId, stepId) >= 0;

        public bool IsStepSkipped(TutorialId tutorialId, StepId stepId) =>
            FindSkippedIndex(tutorialId, stepId) >= 0;

        public bool TryGetChosenBranch(TutorialId tutorialId, StepId fromStepId, out StepId branchStepId)
        {
            for (int i = 0; i < BranchChoices.Count; i++)
            {
                BranchChoiceRecord record = BranchChoices[i];
                if (record.TutorialId == tutorialId.Value && record.FromStepId == fromStepId.Value)
                {
                    branchStepId = new StepId(record.BranchStepId);
                    return true;
                }
            }

            branchStepId = default;
            return false;
        }

        #endregion

        #region Writes

        public void MarkStepCompleted(TutorialId tutorialId, StepId stepId)
        {
            RemoveSkipped(tutorialId, stepId);
            if (FindStepIndex(CompletedSteps, tutorialId, stepId) >= 0) return;

            CompletedSteps.Add(new CompletedStepRecord
            {
                TutorialId = tutorialId.Value,
                StepId = stepId.Value
            });
        }

        public void MarkStepSkipped(TutorialId tutorialId, StepId stepId, SkipReason reason)
        {
            if (FindStepIndex(CompletedSteps, tutorialId, stepId) >= 0) return;

            int index = FindSkippedIndex(tutorialId, stepId);
            var record = new SkippedStepRecord
            {
                TutorialId = tutorialId.Value,
                StepId = stepId.Value,
                Reason = reason
            };

            if (index >= 0)
            {
                SkippedSteps[index] = record;
            }
            else
            {
                SkippedSteps.Add(record);
            }
        }

        public void RecordBranchChoice(TutorialId tutorialId, StepId fromStepId, StepId branchStepId)
        {
            for (int i = 0; i < BranchChoices.Count; i++)
            {
                BranchChoiceRecord existing = BranchChoices[i];
                if (existing.TutorialId == tutorialId.Value && existing.FromStepId == fromStepId.Value)
                {
                    existing.BranchStepId = branchStepId.Value;
                    BranchChoices[i] = existing;
                    return;
                }
            }

            BranchChoices.Add(new BranchChoiceRecord
            {
                TutorialId = tutorialId.Value,
                FromStepId = fromStepId.Value,
                BranchStepId = branchStepId.Value
            });
        }

        public void MarkTutorialCompleted(TutorialId tutorialId)
        {
            if (!CompletedTutorialIds.Contains(tutorialId.Value))
            {
                CompletedTutorialIds.Add(tutorialId.Value);
            }
        }

        #endregion

        #region Maintenance

        public void ClearTutorial(TutorialId tutorialId)
        {
            string id = tutorialId.Value;
            CompletedTutorialIds.Remove(id);
            CompletedSteps.RemoveAll(record => record.TutorialId == id);
            SkippedSteps.RemoveAll(record => record.TutorialId == id);
            BranchChoices.RemoveAll(record => record.TutorialId == id);
        }

        public void Clear()
        {
            CompletedTutorialIds.Clear();
            CompletedSteps.Clear();
            SkippedSteps.Clear();
            BranchChoices.Clear();
        }

        public TutorialProgress DeepCopy()
        {
            return new TutorialProgress
            {
                CompletedTutorialIds = new List<string>(CompletedTutorialIds),
                CompletedSteps = new List<CompletedStepRecord>(CompletedSteps),
                SkippedSteps = new List<SkippedStepRecord>(SkippedSteps),
                BranchChoices = new List<BranchChoiceRecord>(BranchChoices)
            };
        }

        #endregion

        #region Helpers

        private static int FindStepIndex(List<CompletedStepRecord> records, TutorialId tutorialId, StepId stepId)
        {
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i].TutorialId == tutorialId.Value && records[i].StepId == stepId.Value)
                {
                    return i;
                }
            }

            return -1;
        }

        private int FindSkippedIndex(TutorialId tutorialId, StepId stepId)
        {
            for (int i = 0; i < SkippedSteps.Count; i++)
            {
                if (SkippedSteps[i].TutorialId == tutorialId.Value && SkippedSteps[i].StepId == stepId.Value)
                {
                    return i;
                }
            }

            return -1;
        }

        private void RemoveSkipped(TutorialId tutorialId, StepId stepId)
        {
            int index = FindSkippedIndex(tutorialId, stepId);
            if (index >= 0)
            {
                SkippedSteps.RemoveAt(index);
            }
        }

        #endregion
    }

    [Serializable]
    public struct CompletedStepRecord
    {
        public string TutorialId;
        public string StepId;
    }

    [Serializable]
    public struct SkippedStepRecord
    {
        public string TutorialId;
        public string StepId;
        public SkipReason Reason;
    }

    [Serializable]
    public struct BranchChoiceRecord
    {
        public string TutorialId;
        public string FromStepId;
        public string BranchStepId;
    }
}
