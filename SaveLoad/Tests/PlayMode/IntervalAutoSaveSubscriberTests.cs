using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;
using VContainer.Unity;

namespace FakeMG.SaveLoad.Tests.PlayMode
{
    public sealed class IntervalAutoSaveSubscriberTests
    {
        private const string FIXTURE_GUID = "5c64fa917e1c35745935de3a4fb69cf2";
        private const float REMAINING_BEFORE_AUTOSAVE_SECONDS = 0.001f;
        private const float AUTOSAVE_ASSERTION_TIMEOUT_SECONDS = 5f;

        private IObjectResolver _container;
        private IWorldAutoSaveRequester _requester;
        private WorldStartupPolicySO _startupPolicySO;
        private GameObject _instance;
        private CancellationToken _saveToken;
        private bool _hasRequestedSave;
        private UniTaskCompletionSource<WorldSaveResult> _pendingSave;

        #region Public Methods

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _hasRequestedSave = false;
            _pendingSave = new UniTaskCompletionSource<WorldSaveResult>();
            _startupPolicySO = ScriptableObject.CreateInstance<ExplicitWorldSelectionStartupPolicySO>();
            _requester = Substitute.For<IWorldAutoSaveRequester>();
            _requester.TriggerAutoSaveAsync(Arg.Any<CancellationToken>()).Returns(RecordPendingRequest);
            var configuration = new WorldSaveConfiguration(5, WorldSaveConfiguration.MINIMUM_AUTO_SAVE_INTERVAL_SECONDS, 1f, true,
                "Test", _startupPolicySO);
            var builder = new ContainerBuilder();
            builder.RegisterInstance(_requester);
            builder.RegisterInstance(configuration);
            builder.Register<AutoSaveGate>(Lifetime.Singleton);
            builder.Register<IntervalAutoSaveService>(Lifetime.Singleton);
            _container = builder.Build();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(FIXTURE_GUID));
            Assert.That(prefab, Is.Not.Null);
            _instance = _container.Instantiate(prefab);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_instance)
            {
                UnityEngine.Object.Destroy(_instance);
            }
            yield return null;
            _container?.Dispose();
            UnityEngine.Object.Destroy(_startupPolicySO);
        }

        [UnityTest]
        public IEnumerator ActualUpdateSuppressesDueSaveDuringDefeatThenResumesUnscaledScheduleAndCancelsOnDestroy()
        {
            return UniTask.ToCoroutine(async () =>
            {
                float previousTimeScale = Time.timeScale;
                try
                {
                    Time.timeScale = 0f;
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(AUTOSAVE_ASSERTION_TIMEOUT_SECONDS));
                    var autoSave = _container.Resolve<IntervalAutoSaveService>();
                    var configuration = _container.Resolve<WorldSaveConfiguration>();
                    var gate = _container.Resolve<AutoSaveGate>();
                    _requester.HasActiveWorld.Returns(true);
                    await autoSave.AdvanceAsync(configuration.AutoSaveIntervalSeconds - REMAINING_BEFORE_AUTOSAVE_SECONDS, timeout.Token);

                    using (IDisposable suspension = gate.SuspendAutoSave("Defeated colony"))
                    {
                        IDisposable secondSuspension = gate.SuspendAutoSave("World transition");
                        secondSuspension.Dispose();
                        await UniTask.Delay(TimeSpan.FromSeconds(REMAINING_BEFORE_AUTOSAVE_SECONDS), DelayType.UnscaledDeltaTime,
                            cancellationToken: timeout.Token);
                        await UniTask.NextFrame(cancellationToken: timeout.Token);
                        _requester.DidNotReceive().TriggerAutoSaveAsync(Arg.Any<CancellationToken>()).Forget();
                    }
                    await UniTask.WaitUntil(HasRequestedSave, cancellationToken: timeout.Token);
                    _requester.Received(1).TriggerAutoSaveAsync(Arg.Is<CancellationToken>(token => token.CanBeCanceled)).Forget();
                    LogAssert.Expect(LogType.Log, new Regex("Interval autosave wait was cancelled during teardown"));
                    UnityEngine.Object.Destroy(_instance);
                    await UniTask.NextFrame();
                    Assert.That(_saveToken.IsCancellationRequested, Is.True);
                    LogAssert.NoUnexpectedReceived();
                }
                finally
                {
                    Time.timeScale = previousTimeScale;
                }
            });
        }

        #endregion

        #region Private Methods

        private bool HasRequestedSave() => _hasRequestedSave;

        private UniTask<WorldSaveResult> RecordPendingRequest(NSubstitute.Core.CallInfo call)
        {
            _hasRequestedSave = true;
            _saveToken = call.Arg<CancellationToken>();
            return _pendingSave.Task.AttachExternalCancellation(_saveToken);
        }

        #endregion
    }
}
