using System;
using NUnit.Framework;

namespace FakeMG.ActionMapManagement.Tests.EditMode
{
    public sealed class ActionMapSuspensionsTests
    {
        private ActionMapSuspensions _suspensions;
        private int _changeCount;

        [SetUp]
        public void SetUp()
        {
            _suspensions = new ActionMapSuspensions();
            _suspensions.OnChanged += CountChange;
        }

        [TearDown]
        public void TearDown()
        {
            _suspensions.OnChanged -= CountChange;
            _changeCount = 0;
        }

        [Test]
        public void NothingIsSuspendedAtFirst()
        {
            Assert.That(_suspensions.IsSuspended("Movement"), Is.False);
        }

        [Test]
        public void SuspendingNamedMaps_HoldsOnlyThoseMaps()
        {
            _suspensions.Suspend("Defeat", new[] { "Movement" });

            Assert.That(_suspensions.IsSuspended("Movement"), Is.True);
            Assert.That(_suspensions.IsSuspended("UI"), Is.False);
        }

        [Test]
        public void SuspendingAll_HoldsEveryMap()
        {
            _suspensions.SuspendAll("Popup");

            Assert.That(_suspensions.IsSuspended("Anything"), Is.True);
        }

        [Test]
        public void ReleasingTheSuspension_FreesTheMap()
        {
            IDisposable suspension = _suspensions.Suspend("Defeat", new[] { "Movement" });

            suspension.Dispose();

            Assert.That(_suspensions.IsSuspended("Movement"), Is.False);
        }

        [Test]
        public void OverlappingSuspensions_FreeTheMapOnlyOnceAllAreReleased()
        {
            IDisposable popup = _suspensions.SuspendAll("Popup");
            IDisposable defeat = _suspensions.Suspend("Defeat", new[] { "Movement" });

            popup.Dispose();

            Assert.That(_suspensions.IsSuspended("Movement"), Is.True);
            defeat.Dispose();
            Assert.That(_suspensions.IsSuspended("Movement"), Is.False);
        }

        [Test]
        public void SameMapSuspendedTwice_StaysHeldUntilBothRelease()
        {
            IDisposable first = _suspensions.Suspend("First", new[] { "Movement" });
            IDisposable second = _suspensions.Suspend("Second", new[] { "Movement" });

            first.Dispose();

            Assert.That(_suspensions.IsSuspended("Movement"), Is.True);
            second.Dispose();
            Assert.That(_suspensions.IsSuspended("Movement"), Is.False);
        }

        /// <remarks>Regression: a caller disposing both early and on teardown must not free another caller's hold.</remarks>
        [Test]
        public void DisposingTwice_ReleasesOnce()
        {
            IDisposable first = _suspensions.SuspendAll("First");
            _suspensions.SuspendAll("Second");

            first.Dispose();
            first.Dispose();

            Assert.That(_suspensions.IsSuspended("Movement"), Is.True);
        }

        [Test]
        public void SuspendingAndReleasing_EachRaiseChanged()
        {
            IDisposable suspension = _suspensions.SuspendAll("Popup");

            suspension.Dispose();

            Assert.That(_changeCount, Is.EqualTo(2));
        }

        [Test]
        public void SuspendingNoMaps_HoldsNothing()
        {
            _suspensions.Suspend("Empty", Array.Empty<string>());

            Assert.That(_suspensions.IsSuspended("Movement"), Is.False);
        }

        private void CountChange()
        {
            _changeCount++;
        }
    }
}
