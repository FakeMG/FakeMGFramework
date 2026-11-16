using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace FakeMG.Tutorial
{
    public sealed class TutorialStarter : MonoBehaviour
    {
        [SerializeField] private bool _startOnStart = true;
        [SerializeField] private MonoBehaviour _sequenceSource;

        private TutorialService _tutorialService;
        private CancellationTokenSource _cancellationTokenSource;

        #region Unity Lifecycle

        private void Start()
        {
            if (_startOnStart)
            {
                StartTutorial();
            }
        }

        private void OnDestroy()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }

        #endregion

        #region Public Methods

        [Inject]
        public void Construct(TutorialService tutorialService)
        {
            _tutorialService = tutorialService;
        }

        public void StartTutorial()
        {
            var sequence = (ITutorialSequence)_sequenceSource;

            _cancellationTokenSource = new CancellationTokenSource();
            _tutorialService.StartAsync(sequence, forceReplay: false, _cancellationTokenSource.Token).Forget();
        }

        #endregion
    }
}
