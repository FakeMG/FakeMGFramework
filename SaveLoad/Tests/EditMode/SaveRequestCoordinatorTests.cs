using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.SaveLoad;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace FakeMG.SaveLoad.Tests
{
    /// <summary>
    /// Verifies shared save request ownership: requests coalesce, higher-priority same-frame requests
    /// win, and canceling one caller never cancels the operation observed by another caller.
    /// </summary>
    public sealed class SaveRequestCoordinatorTests
    {
        [UnityTest]
        public IEnumerator RequestAsync_TwoConcurrentCallers_ExecutesOneOperation()
        {
            return UniTask.ToCoroutine(async () =>
            {
                var operationGate =
                    new UniTaskCompletionSource<bool>();
                int operationCount = 0;
                SaveFileKind executedKind = SaveFileKind.Unknown;

                async UniTask<bool> ExecuteAsync(
                    SaveFileKind saveKind,
                    CancellationToken cancellationToken)
                {
                    operationCount++;
                    executedKind = saveKind;
                    return await operationGate.Task;
                }

                var coordinator = new SaveRequestCoordinator(
                    ExecuteAsync,
                    CancellationToken.None);
                UniTask<bool> autoSave = coordinator.RequestAsync(
                    SaveFileKind.Auto,
                    CancellationToken.None);
                UniTask<bool> manualSave = coordinator.RequestAsync(
                    SaveFileKind.Manual,
                    CancellationToken.None);
                await UniTask.Yield();
                operationGate.TrySetResult(true);

                (bool autoSaveResult, bool manualSaveResult) =
                    await UniTask.WhenAll(
                    autoSave,
                    manualSave);

                Assert.That(operationCount, Is.EqualTo(1));
                Assert.That(executedKind, Is.EqualTo(SaveFileKind.Manual));
                Assert.That(autoSaveResult, Is.True);
                Assert.That(manualSaveResult, Is.True);
            });
        }

        [UnityTest]
        public IEnumerator RequestAsync_FirstCallerCancels_SecondCallerStillReceivesCommit()
        {
            return UniTask.ToCoroutine(async () =>
            {
                var operationGate =
                    new UniTaskCompletionSource<bool>();

                async UniTask<bool> ExecuteAsync(
                    SaveFileKind saveKind,
                    CancellationToken cancellationToken)
                {
                    return await operationGate.Task;
                }

                var coordinator = new SaveRequestCoordinator(
                    ExecuteAsync,
                    CancellationToken.None);
                using var firstCallerCancellationSource =
                    new CancellationTokenSource();
                UniTask<bool> firstWait = coordinator.RequestAsync(
                    SaveFileKind.Manual,
                    firstCallerCancellationSource.Token);
                UniTask<bool> lifecycleWait = coordinator.RequestAsync(
                    SaveFileKind.Fixed,
                    CancellationToken.None);
                firstCallerCancellationSource.Cancel();
                await UniTask.Yield();

                bool wasFirstWaitCanceled = false;
                try
                {
                    await firstWait;
                }
                catch (System.OperationCanceledException)
                {
                    wasFirstWaitCanceled = true;
                }

                Assert.That(wasFirstWaitCanceled, Is.True);
                operationGate.TrySetResult(true);

                Assert.That(await lifecycleWait, Is.True);
            });
        }
    }
}
