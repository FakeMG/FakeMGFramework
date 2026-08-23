using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace FakeMG.SaveLoad.Tests
{
    public sealed class SaveParticipantOrderResolverTests
    {
        [Test]
        public void Resolve_DeclaredDependencies_ReturnsStableTopologicalOrder()
        {
            var first = new TestParticipant("first");
            var second = new TestParticipant("second", "first");
            var third = new TestParticipant("third", "second");

            IReadOnlyList<IAsyncSaveParticipant> orderedParticipants =
                new AsyncSaveParticipantDependencyOrderResolver().Resolve(new[] { third, first, second });

            Assert.That(
                orderedParticipants,
                Is.EqualTo(new IAsyncSaveParticipant[] { first, second, third }));
        }

        [Test]
        public void Resolve_MissingDependency_ThrowsInvalidOperationException()
        {
            var participant = new TestParticipant("participant", "missing");

            Assert.Throws<InvalidOperationException>(() =>
                new AsyncSaveParticipantDependencyOrderResolver().Resolve(new[] { participant }));
        }

        [Test]
        public void Resolve_DependencyCycle_ThrowsInvalidOperationException()
        {
            var first = new TestParticipant("first", "second");
            var second = new TestParticipant("second", "first");

            Assert.Throws<InvalidOperationException>(() =>
                new AsyncSaveParticipantDependencyOrderResolver().Resolve(new[] { first, second }));
        }

        [Test]
        public void Resolve_DuplicateParticipantIds_ThrowsInvalidOperationException()
        {
            var first = new TestParticipant("duplicate");
            var second = new TestParticipant("duplicate");

            Assert.Throws<InvalidOperationException>(() =>
                new AsyncSaveParticipantDependencyOrderResolver().Resolve(new[] { first, second }));
        }

        private sealed class TestParticipant : IAsyncSaveParticipant
        {
            public string ParticipantId { get; }
            public IReadOnlyCollection<string> RunsAfterParticipantIds { get; }

            public TestParticipant(string participantId, params string[] dependencyIds)
            {
                ParticipantId = participantId;
                RunsAfterParticipantIds = dependencyIds;
            }

            public UniTask PrepareSaveAsync(
                SaveOperationContext context,
                CancellationToken cancellationToken)
            {
                return UniTask.CompletedTask;
            }

            public UniTask ApplyLoadedStateAsync(
                LoadOperationContext context,
                CancellationToken cancellationToken)
            {
                return UniTask.CompletedTask;
            }

            public UniTask RollBackLoadedStateAsync(
                LoadOperationContext context,
                CancellationToken cancellationToken)
            {
                return UniTask.CompletedTask;
            }

            public UniTask CompleteSaveAsync(
                SaveOperationContext context,
                bool didMetadataCommit,
                CancellationToken cancellationToken)
            {
                return UniTask.CompletedTask;
            }
        }
    }
}
