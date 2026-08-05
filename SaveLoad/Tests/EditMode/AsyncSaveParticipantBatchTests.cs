using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.SaveLoad;
using NSubstitute;
using NUnit.Framework;

namespace FakeMG.SaveLoad.Tests
{
    /// <summary>
    /// Verifies ordered participant preparation and reverse cleanup when a participant fails midway.
    /// </summary>
    public sealed class AsyncSaveParticipantBatchTests
    {
        [Test]
        public async System.Threading.Tasks.Task PrepareAsync_SecondParticipantFails_CompletesOnlyEnteredParticipantsInReverse()
        {
            var completionOrder = new List<string>();
            IAsyncSaveParticipant first = CreateParticipant("first", completionOrder);
            IAsyncSaveParticipant failing = CreateParticipant("failing", completionOrder);
            IAsyncSaveParticipant neverEntered = CreateParticipant("never", completionOrder);

            failing.PrepareSaveAsync(Arg.Any<SaveOperationContext>(), Arg.Any<CancellationToken>())
                .Returns(UniTask.FromException(new InvalidOperationException("prepare failed")));

            var batch = new AsyncSaveParticipantBatch();
            var context = new SaveOperationContext(string.Empty, "test.es3", SaveFileKind.Fixed, DateTime.UtcNow);

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await batch.PrepareAsync(new[] { first, failing, neverEntered }, context, CancellationToken.None));

            await batch.CompleteAsync(context, false, CancellationToken.None, null);

            CollectionAssert.AreEqual(new[] { "failing", "first" }, completionOrder);

            await neverEntered.DidNotReceive().CompleteSaveAsync(Arg.Any<SaveOperationContext>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        }

        private static IAsyncSaveParticipant CreateParticipant(
            string participantName,
            ICollection<string> completionOrder)
        {
            IAsyncSaveParticipant participant = Substitute.For<IAsyncSaveParticipant>();
            participant.PrepareSaveAsync(Arg.Any<SaveOperationContext>(), Arg.Any<CancellationToken>())
                .Returns(UniTask.CompletedTask);
            participant.CompleteSaveAsync(Arg.Any<SaveOperationContext>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    completionOrder.Add(participantName);
                    return UniTask.CompletedTask;
                });
            return participant;
        }
    }
}
