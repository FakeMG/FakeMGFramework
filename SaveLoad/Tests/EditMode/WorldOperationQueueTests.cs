using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace FakeMG.SaveLoad.Tests
{
    public sealed class WorldOperationQueueTests
    {
        [Test]
        public async System.Threading.Tasks.Task EnqueueSaveAsync_ManualDuringActiveAutoSave_RunsManualBeforePendingAutoSave()
        {
            using var operationQueue = new WorldOperationQueue();
            var activeAutoSaveGate = new UniTaskCompletionSource();
            List<string> executionOrder = new();
            UniTask<WorldSaveResult> activeAutoSave = operationQueue.EnqueueSaveAsync(
                WorldSnapshotKind.Auto,
                async cancellationToken =>
                {
                    executionOrder.Add("active-auto");
                    await activeAutoSaveGate.Task.AttachExternalCancellation(cancellationToken);
                    return CreateSuccess();
                },
                CancellationToken.None).Preserve();
            UniTask<WorldSaveResult> pendingAutoSave = operationQueue.EnqueueSaveAsync(
                WorldSnapshotKind.Auto,
                cancellationToken =>
                {
                    executionOrder.Add("pending-auto");
                    return UniTask.FromResult(CreateSuccess());
                },
                CancellationToken.None).Preserve();
            UniTask<WorldSaveResult> manualSave = operationQueue.EnqueueSaveAsync(
                WorldSnapshotKind.Manual,
                cancellationToken =>
                {
                    executionOrder.Add("manual");
                    return UniTask.FromResult(CreateSuccess());
                },
                CancellationToken.None).Preserve();

            activeAutoSaveGate.TrySetResult();
            await UniTask.WhenAll(new[] { activeAutoSave, pendingAutoSave, manualSave });

            Assert.That(executionOrder, Is.EqualTo(new[] { "active-auto", "manual", "pending-auto" }));
        }

        [Test]
        public async System.Threading.Tasks.Task EnqueueSaveAsync_MultiplePendingAutoSaves_CoalescesOnlyAutosaves()
        {
            using var operationQueue = new WorldOperationQueue();
            var activeGate = new UniTaskCompletionSource();
            int autoSaveExecutionCount = 0;
            int manualSaveExecutionCount = 0;
            UniTask<WorldSaveResult> activeOperation = operationQueue.EnqueueSaveAsync(
                WorldSnapshotKind.Manual,
                async cancellationToken =>
                {
                    await activeGate.Task.AttachExternalCancellation(cancellationToken);
                    manualSaveExecutionCount++;
                    return CreateSuccess();
                },
                CancellationToken.None).Preserve();
            UniTask<WorldSaveResult> firstAutoSave = operationQueue.EnqueueSaveAsync(
                WorldSnapshotKind.Auto,
                cancellationToken =>
                {
                    autoSaveExecutionCount++;
                    return UniTask.FromResult(CreateSuccess());
                },
                CancellationToken.None).Preserve();
            UniTask<WorldSaveResult> secondAutoSave = operationQueue.EnqueueSaveAsync(
                WorldSnapshotKind.Auto,
                cancellationToken =>
                {
                    autoSaveExecutionCount++;
                    return UniTask.FromResult(CreateSuccess());
                },
                CancellationToken.None).Preserve();
            UniTask<WorldSaveResult> secondManualSave = operationQueue.EnqueueSaveAsync(
                WorldSnapshotKind.Manual,
                cancellationToken =>
                {
                    manualSaveExecutionCount++;
                    return UniTask.FromResult(CreateSuccess());
                },
                CancellationToken.None).Preserve();

            activeGate.TrySetResult();
            await UniTask.WhenAll(new[]
            {
                activeOperation,
                firstAutoSave,
                secondAutoSave,
                secondManualSave,
            });

            Assert.That(autoSaveExecutionCount, Is.EqualTo(1));
            Assert.That(manualSaveExecutionCount, Is.EqualTo(2));
        }

        private static WorldSaveResult CreateSuccess()
        {
            return new WorldSaveResult(WorldSaveStatus.Success, "snapshot.sav", string.Empty, true);
        }
    }
}
