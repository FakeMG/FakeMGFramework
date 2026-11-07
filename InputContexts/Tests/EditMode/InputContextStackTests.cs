using NUnit.Framework;
using UnityEngine.InputSystem;

namespace FakeMG.InputContexts.Tests.EditMode
{
    public sealed class InputContextStackTests
    {
        private InputContextTestFactory _factory;
        private InputContextStack _stack;
        private InputAction _back;

        [SetUp]
        public void SetUp()
        {
            _factory = new InputContextTestFactory();
            _stack = new InputContextStack();
            _back = _factory.CreateAction("Back");
        }

        [TearDown]
        public void TearDown()
        {
            _factory.DestroyAll();
        }

        [Test]
        public void ActiveContexts_ListNewestFirst()
        {
            var screen = new FakeInputContext(false);
            var overlay = new FakeInputContext(false);

            _stack.Show(screen);
            _stack.Show(overlay);

            CollectionAssert.AreEqual(new[] { overlay, screen }, _stack.ActiveContexts);
        }

        [Test]
        public void ModalContext_SilencesEveryContextShownBeforeIt()
        {
            var screen = new FakeInputContext(false);
            var dialog = new FakeInputContext(true);

            _stack.Show(screen);
            _stack.Show(dialog);

            CollectionAssert.AreEqual(new[] { dialog }, _stack.ActiveContexts);
        }

        [Test]
        public void HidingModalContext_RevealsTheContextsBeneathIt()
        {
            var screen = new FakeInputContext(false);
            var dialog = new FakeInputContext(true);
            _stack.Show(screen);
            _stack.Show(dialog);

            _stack.Hide(dialog);

            CollectionAssert.AreEqual(new[] { screen }, _stack.ActiveContexts);
        }

        [Test]
        public void SharedAction_BelongsToTheNewestContextAnsweringIt()
        {
            var screen = new FakeInputContext(false);
            var overlay = new FakeInputContext(false);
            screen.Handlers.AddHandler(_back, DoNothing);
            overlay.Handlers.AddHandler(_back, DoNothing);
            _stack.Show(screen);
            _stack.Show(overlay);

            Assert.That(_stack.TryFindOwner(_back, out IInputContext owner), Is.True);
            Assert.That(owner, Is.SameAs(overlay));
        }

        [Test]
        public void NewerContextWithoutAHandler_LeavesTheActionToTheOneBeneath()
        {
            var screen = new FakeInputContext(false);
            screen.Handlers.AddHandler(_back, DoNothing);
            _stack.Show(screen);
            _stack.Show(new FakeInputContext(false));

            _stack.TryFindOwner(_back, out IInputContext owner);

            Assert.That(owner, Is.SameAs(screen));
        }

        [Test]
        public void ActionAnsweredOnlyBeneathAModal_HasNoOwner()
        {
            var screen = new FakeInputContext(false);
            screen.Handlers.AddHandler(_back, DoNothing);
            _stack.Show(screen);
            _stack.Show(new FakeInputContext(true));

            Assert.That(_stack.TryFindOwner(_back, out _), Is.False);
        }

        [Test]
        public void AnsweringAnActionInAShownContext_ReportsAnOwnershipChange()
        {
            var screen = new FakeInputContext(false);
            _stack.Show(screen);
            int changeCount = 0;
            _stack.OnOwnedActionsChanged += () => changeCount++;

            screen.Handlers.AddHandler(_back, DoNothing);

            Assert.That(changeCount, Is.EqualTo(1));
        }

        [Test]
        public void AnsweringAnActionInAHiddenContext_ReportsNothing()
        {
            var screen = new FakeInputContext(false);
            _stack.Show(screen);
            _stack.Hide(screen);
            int changeCount = 0;
            _stack.OnOwnedActionsChanged += () => changeCount++;

            screen.Handlers.AddHandler(_back, DoNothing);

            Assert.That(changeCount, Is.Zero);
        }

        [Test]
        public void ShowingAContextTwice_ListsItOnce()
        {
            var screen = new FakeInputContext(false);

            _stack.Show(screen);
            _stack.Show(screen);

            CollectionAssert.AreEqual(new[] { screen }, _stack.ActiveContexts);
        }

        private static void DoNothing()
        {
        }
    }
}
