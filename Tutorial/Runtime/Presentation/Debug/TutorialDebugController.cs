using FakeMG.Framework;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace FakeMG.Tutorial
{
    public sealed class TutorialDebugController : MonoBehaviour
    {
        [SerializeField] private string _tutorialId;
        [SerializeField] private List<string> _precedingStepIds = new();
        [SerializeField] private MonoBehaviour _sequenceSource;

        private TutorialProgressStore _store;
        private TutorialService _service;
        private TutorialForceCompleteSignal _forceCompleteSignal;

        #region Public Methods

        [Inject]
        public void Construct(TutorialProgressStore store, TutorialService service,
            TutorialForceCompleteSignal forceCompleteSignal)
        {
            _store = store;
            _service = service;
            _forceCompleteSignal = forceCompleteSignal;
        }

        [Button]
        public void ForceCompleteCurrentStep()
        {
            Echo.Log("Debug: force-completing the current tutorial step.");
            _forceCompleteSignal.ForceCompleteCurrentStep();
        }

        [Button]
        public void ResetTutorial()
        {
            var tutorialId = new TutorialId(_tutorialId);
            Echo.Log($"Debug: resetting saved progress for tutorial '{tutorialId}'.");
            _store.ResetTutorial(tutorialId);
        }

        [Button]
        public void ResetAll()
        {
            Echo.Log("Debug: resetting all saved tutorial progress.");
            _store.ResetAll();
        }

        [Button]
        public void PrepareJumpToStep()
        {
            var tutorialId = new TutorialId(_tutorialId);
            for (int stepIndex = 0; stepIndex < _precedingStepIds.Count; stepIndex++)
            {
                _store.ForceCompleteStep(tutorialId, new StepId(_precedingStepIds[stepIndex]));
            }

            Echo.Log($"Debug: marked {_precedingStepIds.Count} step(s) completed to jump ahead in '{tutorialId}'.");
        }

        [Button]
        public void Replay()
        {
            if (_sequenceSource is not ITutorialSequence sequence)
            {
                Echo.Error("Debug: cannot replay the tutorial because the configured sequence source does not implement ITutorialSequence.");
                return;
            }

            Echo.Log($"Debug: replaying tutorial '{sequence.Id}' without changing saved progress.");
            _service.StartAsync(sequence, forceReplay: true, this.GetCancellationTokenOnDestroy()).Forget();
        }

        #endregion
    }
}
