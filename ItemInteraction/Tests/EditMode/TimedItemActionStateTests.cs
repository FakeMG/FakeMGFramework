using FakeMG.ItemInteraction;
using NSubstitute;
using NUnit.Framework;

namespace FakeMG.ItemInteraction.Tests
{
    public sealed class TimedItemActionStateTests
    {
        #region Public Methods

        [Test]
        public void PressToUse_ReleasedBeforeMoment_RequestsCommitExactlyOnceAtMoment()
        {
            IItemInputLifecycle lifecycle = CreateLifecycle(false, true);
            var state = new TimedItemActionState(lifecycle, 0.65f, 1f);

            state.Release();
            Assert.That(state.Tick(0.64f).ShouldAttemptCommit, Is.False);
            Assert.That(state.Tick(0.01f).ShouldAttemptCommit, Is.True);
            Assert.That(state.Tick(2f).ShouldAttemptCommit, Is.False);
            Assert.That(state.IsActive, Is.False);
        }

        [Test]
        public void HoldToConfirm_ReleasedBeforeMoment_Cancels()
        {
            IItemInputLifecycle lifecycle = CreateLifecycle(true, false);
            var state = new TimedItemActionState(lifecycle, 0.5f, 1f);

            state.Tick(0.49f);
            TimedItemActionOutcome releaseOutcome = state.Release();

            Assert.That(releaseOutcome.HasCanceled, Is.True);
            Assert.That(state.Tick(1f).ShouldAttemptCommit, Is.False);
            Assert.That(state.IsActive, Is.False);
        }

        [Test]
        public void LargeDelta_CrossingMomentAndCompletion_RequestsOneCommitAndCompletes()
        {
            IItemInputLifecycle lifecycle = CreateLifecycle(false, true);
            var state = new TimedItemActionState(lifecycle, 0.5f, 1f);

            TimedItemActionOutcome outcome = state.Tick(2f);

            Assert.That(outcome.ShouldAttemptCommit, Is.True);
            Assert.That(outcome.HasCompleted, Is.True);
            Assert.That(state.Tick(2f).ShouldAttemptCommit, Is.False);
        }

        [Test]
        public void CancellationAfterCommitRequest_DoesNotRequestAnotherCommit()
        {
            IItemInputLifecycle lifecycle = CreateLifecycle(false, true);
            var state = new TimedItemActionState(lifecycle, 0.5f, 1f);

            Assert.That(state.Tick(0.5f).ShouldAttemptCommit, Is.True);
            state.Cancel();

            Assert.That(state.Tick(5f).ShouldAttemptCommit, Is.False);
        }

        #endregion

        #region Private Methods

        private static IItemInputLifecycle CreateLifecycle(bool shouldCancelOnEarlyRelease, bool canCommit)
        {
            IItemInputLifecycle lifecycle = Substitute.For<IItemInputLifecycle>();
            lifecycle.ShouldCancelWhenReleasedBeforeActionMoment.Returns(shouldCancelOnEarlyRelease);
            lifecycle.CanCommitAtActionMoment(Arg.Any<bool>()).Returns(canCommit);
            return lifecycle;
        }

        #endregion
    }
}
