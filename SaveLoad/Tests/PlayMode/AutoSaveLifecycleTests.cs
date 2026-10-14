using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;

namespace FakeMG.SaveLoad.Tests.PlayMode
{
    public sealed class AutoSaveLifecycleTests
    {
        private IObjectResolver _container;
        private IWorldAutoSaveRequester _requester;
        private WorldLifecycleAutoSave _lifecycle;
        private IntervalAutoSaveService _interval;
        private WorldStartupPolicySO _startupPolicySO;
        private UniTaskCompletionSource<WorldSaveResult> _pendingSave;
        private CancellationToken _saveToken;

        #region Public Methods

        [SetUp]
        public void SetUp()
        {
            _startupPolicySO = ScriptableObject.CreateInstance<ExplicitWorldSelectionStartupPolicySO>();
            _requester = Substitute.For<IWorldAutoSaveRequester>();
            _requester.HasActiveWorld.Returns(true);
            _requester.TriggerAutoSaveAsync(Arg.Any<CancellationToken>()).Returns(UniTask.FromResult(Success()));
            var configuration = new WorldSaveConfiguration(5, 30f, 0.1f, true, "Test", _startupPolicySO);
            var builder = new ContainerBuilder();
            builder.RegisterInstance(_requester);
            builder.RegisterInstance(configuration);
            builder.Register<WorldLifecycleAutoSave>(Lifetime.Singleton);
            builder.Register<IntervalAutoSaveService>(Lifetime.Singleton);
            _container = builder.Build();
            _lifecycle = _container.Resolve<WorldLifecycleAutoSave>();
            _interval = _container.Resolve<IntervalAutoSaveService>();
        }

        [TearDown]
        public void TearDown()
        {
            _container?.Dispose();
            UnityEngine.Object.DestroyImmediate(_startupPolicySO);
        }

        [UnityTest]
        public IEnumerator FocusRegainAndFocusLossWithoutWorldDoNotRequestSave()
        {
            return UniTask.ToCoroutine(async () =>
            {
                await _lifecycle.FocusChangedAsync(true, CancellationToken.None);
                _requester.HasActiveWorld.Returns(false);
                await _lifecycle.FocusChangedAsync(false, CancellationToken.None);
                _requester.DidNotReceive().TriggerAutoSaveAsync(Arg.Any<CancellationToken>()).Forget();
            });
        }

        [UnityTest]
        public IEnumerator FocusLossRequestsOneSaveAndReleasesItsTimeoutLifetime()
        {
            return UniTask.ToCoroutine(async () =>
            {
                _requester.TriggerAutoSaveAsync(Arg.Any<CancellationToken>()).Returns(CaptureSuccessfulSave);
                await _lifecycle.FocusChangedAsync(false, CancellationToken.None);
                _requester.Received(1).TriggerAutoSaveAsync(Arg.Is<CancellationToken>(token => token.CanBeCanceled)).Forget();
                Assert.That(_saveToken.IsCancellationRequested, Is.True);
            });
        }

        [UnityTest]
        public IEnumerator FailedFocusSaveReportsReturnedReason()
        {
            return UniTask.ToCoroutine(async () =>
            {
                _requester.TriggerAutoSaveAsync(Arg.Any<CancellationToken>()).Returns(UniTask.FromResult(Failure()));
                LogAssert.Expect(LogType.Error, new Regex("World autosave failed during application focus loss: Disk unavailable"));
                await _lifecycle.FocusChangedAsync(false, CancellationToken.None);
            });
        }

        [UnityTest]
        public IEnumerator ThrownFocusSaveErrorIsReported()
        {
            return UniTask.ToCoroutine(async () =>
            {
                _requester.TriggerAutoSaveAsync(Arg.Any<CancellationToken>())
                    .Returns(UniTask.FromException<WorldSaveResult>(new IOException("Save threw.")));
                LogAssert.Expect(LogType.Error, new Regex("World autosave failed during application focus loss:.*Save threw"));
                await _lifecycle.FocusChangedAsync(false, CancellationToken.None);
            });
        }

        [UnityTest]
        public IEnumerator FocusFlushTimeoutUsesUnscaledTimeAndCancelsPendingSave()
        {
            return UniTask.ToCoroutine(async () =>
            {
                ConfigurePendingSave();
                float previousTimeScale = Time.timeScale;
                try
                {
                    Time.timeScale = 0f;
                    LogAssert.Expect(LogType.Error, new Regex("World autosave exceeded.*application focus loss"));
                    await _lifecycle.FocusChangedAsync(false, CancellationToken.None);
                    Assert.That(_saveToken.IsCancellationRequested, Is.True);
                }
                finally
                {
                    Time.timeScale = previousTimeScale;
                }
            });
        }

        [UnityTest]
        public IEnumerator FocusFlushTeardownCancellationStopsPendingSaveWithoutError()
        {
            return UniTask.ToCoroutine(async () =>
            {
                ConfigurePendingSave();
                using var cancellation = new CancellationTokenSource();
                UniTask flush = _lifecycle.FocusChangedAsync(false, cancellation.Token);
                LogAssert.Expect(LogType.Log, new Regex("World autosave during application focus loss was cancelled during teardown"));
                cancellation.Cancel();
                await flush;
                Assert.That(_saveToken.IsCancellationRequested, Is.True);
                LogAssert.NoUnexpectedReceived();
            });
        }

        [UnityTest]
        public IEnumerator IntervalSuspensionFreezesCountdownThenResumesOneRequestAtTheThreshold()
        {
            return UniTask.ToCoroutine(async () =>
            {
                await _interval.AdvanceAsync(29f, CancellationToken.None);
                using (var suspension = _interval.SuspendIntervalAutoSave("Defeat"))
                {
                    await _interval.AdvanceAsync(300f, CancellationToken.None);
                    _requester.DidNotReceive().TriggerAutoSaveAsync(Arg.Any<CancellationToken>()).Forget();
                }
                using var cancellation = new CancellationTokenSource();
                await _interval.AdvanceAsync(1f, cancellation.Token);
                _requester.Received(1).TriggerAutoSaveAsync(cancellation.Token).Forget();
                await _interval.AdvanceAsync(0f, cancellation.Token);
                _requester.Received(1).TriggerAutoSaveAsync(Arg.Any<CancellationToken>()).Forget();
            });
        }

        [UnityTest]
        public IEnumerator IntervalWithoutActiveWorldDoesNotConsumeCountdown()
        {
            return UniTask.ToCoroutine(async () =>
            {
                _requester.HasActiveWorld.Returns(false);
                await _interval.AdvanceAsync(300f, CancellationToken.None);
                _requester.DidNotReceive().TriggerAutoSaveAsync(Arg.Any<CancellationToken>()).Forget();
                _requester.HasActiveWorld.Returns(true);
                await _interval.AdvanceAsync(29f, CancellationToken.None);
                _requester.DidNotReceive().TriggerAutoSaveAsync(Arg.Any<CancellationToken>()).Forget();
                await _interval.AdvanceAsync(1f, CancellationToken.None);
                _requester.Received(1).TriggerAutoSaveAsync(CancellationToken.None).Forget();
            });
        }

        [UnityTest]
        public IEnumerator FailedIntervalSaveReportsReturnedReasonAndRetainsSchedule()
        {
            return UniTask.ToCoroutine(async () =>
            {
                _requester.TriggerAutoSaveAsync(Arg.Any<CancellationToken>()).Returns(UniTask.FromResult(Failure()));
                LogAssert.Expect(LogType.Error, new Regex("Interval autosave failed: Disk unavailable"));
                await _interval.AdvanceAsync(30f, CancellationToken.None);
                _requester.TriggerAutoSaveAsync(Arg.Any<CancellationToken>()).Returns(UniTask.FromResult(Success()));
                await _interval.AdvanceAsync(30f, CancellationToken.None);
                _requester.Received(2).TriggerAutoSaveAsync(CancellationToken.None).Forget();
            });
        }

        [UnityTest]
        public IEnumerator ThrownIntervalSaveErrorIsReported()
        {
            return UniTask.ToCoroutine(async () =>
            {
                _requester.TriggerAutoSaveAsync(Arg.Any<CancellationToken>())
                    .Returns(UniTask.FromException<WorldSaveResult>(new IOException("Save threw.")));
                LogAssert.Expect(LogType.Error, new Regex("Interval autosave failed:.*Save threw"));
                await _interval.AdvanceAsync(30f, CancellationToken.None);
            });
        }

        [UnityTest]
        public IEnumerator IntervalTeardownCancellationStopsWaitWithoutError()
        {
            return UniTask.ToCoroutine(async () =>
            {
                ConfigurePendingSave();
                using var cancellation = new CancellationTokenSource();
                UniTask save = _interval.AdvanceAsync(30f, cancellation.Token);
                LogAssert.Expect(LogType.Log, new Regex("Interval autosave wait was cancelled during teardown"));
                cancellation.Cancel();
                await save;
                Assert.That(_saveToken.IsCancellationRequested, Is.True);
                LogAssert.NoUnexpectedReceived();
            });
        }

        #endregion

        #region Private Methods

        private void ConfigurePendingSave()
        {
            _pendingSave = new UniTaskCompletionSource<WorldSaveResult>();
            _requester.TriggerAutoSaveAsync(Arg.Any<CancellationToken>()).Returns(CapturePendingSave);
        }

        private UniTask<WorldSaveResult> CapturePendingSave(NSubstitute.Core.CallInfo call)
        {
            _saveToken = call.Arg<CancellationToken>();
            return _pendingSave.Task.AttachExternalCancellation(_saveToken);
        }

        private UniTask<WorldSaveResult> CaptureSuccessfulSave(NSubstitute.Core.CallInfo call)
        {
            _saveToken = call.Arg<CancellationToken>();
            return UniTask.FromResult(Success());
        }

        private static WorldSaveResult Success() => new(WorldSaveStatus.Success, "autosave.sav", "", true);
        private static WorldSaveResult Failure() => new(WorldSaveStatus.SnapshotFailed, "", "Disk unavailable.", false);

        #endregion
    }
}
