using FakeMG.Framework;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework.EventBus;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Public entry point for starting tutorials. Enforces a single active sequence,
    /// skips already-completed tutorials unless a forced replay is requested, and routes
    /// a forced replay through a throwaway progress copy so saved progress is never
    /// changed.
    /// </summary>
    public sealed class TutorialService
    {
        private readonly TutorialRunner _runner;
        private readonly TutorialProgressStore _store;

        private bool _isActive;

        public TutorialService(TutorialRunner runner, TutorialProgressStore store)
        {
            _runner = runner;
            _store = store;
        }

        public bool IsActive => _isActive;

        #region Public Methods

        public async UniTask StartAsync(ITutorialSequence sequence, bool forceReplay = false,
            CancellationToken cancellationToken = default)
        {
            if (_isActive)
            {
                Echo.Warning($"Tutorial start request for '{sequence.Id}' ignored: another tutorial is already active.");
                return;
            }

            if (!forceReplay && _store.Saved.IsTutorialCompleted(sequence.Id))
            {
                Echo.Log($"Tutorial '{sequence.Id}' is already completed; not starting. Request a force replay to run it again.");
                // Still signal the ended state so listeners that gate behavior on the tutorial
                // being over (e.g. returning players) react the same as a fresh completion.
                EventBus<TutorialEndedEvent>.Raise(new TutorialEndedEvent
                {
                    TutorialId = sequence.Id,
                    ReachedValidEndState = true,
                    IsForcedReplay = false
                });
                return;
            }

            TutorialProgress progress = forceReplay
                ? _store.BeginReplaySession()
                : _store.BeginNormalSession();

            _isActive = true;
            try
            {
                await _runner.RunAsync(sequence, progress, forceReplay, cancellationToken);
            }
            finally
            {
                _isActive = false;
            }
        }

        #endregion
    }
}
