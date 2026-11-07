using NUnit.Framework;
using UnityEngine.InputSystem;

namespace FakeMG.InputContexts.Tests.EditMode
{
    public sealed class InputContextRouterTests : InputTestFixture
    {
        private InputContextTestFactory _factory;
        private InputContextStack _stack;
        private InputContextRouter _router;
        private Keyboard _keyboard;
        private InputAction _back;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _factory = new InputContextTestFactory();
            _back = _factory.CreateAction("Back", "<Keyboard>/f");
            _back.Enable();
            _stack = new InputContextStack();
            _router = new InputContextRouter(_stack);
            _router.Initialize();
        }

        [TearDown]
        public override void TearDown()
        {
            _router.Dispose();
            _factory.DestroyAll();
            base.TearDown();
        }

        [Test]
        public void PressingAnActionOfAnActiveContext_RunsItsHandler()
        {
            var screen = new FakeInputContext(false);
            int handledCount = 0;
            screen.Handlers.AddHandler(_back, () => handledCount++);
            _stack.Show(screen);

            PressAndRelease(_keyboard.fKey);

            Assert.That(handledCount, Is.EqualTo(1));
        }

        [Test]
        public void HandlerAddedAfterTheContextShows_StillRuns()
        {
            var screen = new FakeInputContext(false);
            bool isHandled = false;
            _stack.Show(screen);
            screen.Handlers.AddHandler(_back, () => isHandled = true);

            PressAndRelease(_keyboard.fKey);

            Assert.That(isHandled, Is.True);
        }

        [Test]
        public void ContextBeneathAModal_DoesNotRespond()
        {
            var screen = new FakeInputContext(false);
            bool isScreenHandled = false;
            screen.Handlers.AddHandler(_back, () => isScreenHandled = true);
            _stack.Show(screen);
            _stack.Show(new FakeInputContext(true));

            PressAndRelease(_keyboard.fKey);

            Assert.That(isScreenHandled, Is.False);
        }

        [Test]
        public void HiddenContext_DoesNotRespond()
        {
            var screen = new FakeInputContext(false);
            bool isHandled = false;
            screen.Handlers.AddHandler(_back, () => isHandled = true);
            _stack.Show(screen);
            _stack.Hide(screen);

            PressAndRelease(_keyboard.fKey);

            Assert.That(isHandled, Is.False);
        }

        [Test]
        public void BackThatClosesADialog_DoesNotAlsoReachTheScreenBeneath()
        {
            var screen = new FakeInputContext(false);
            var dialog = new FakeInputContext(true);
            bool isScreenHandled = false;
            screen.Handlers.AddHandler(_back, () => isScreenHandled = true);
            dialog.Handlers.AddHandler(_back, () => _stack.Hide(dialog));
            _stack.Show(screen);
            _stack.Show(dialog);

            PressAndRelease(_keyboard.fKey);

            Assert.That(isScreenHandled, Is.False);
        }

        /// <remarks>Regression: reading the stale callback context threw IndexOutOfRangeException when the Build menu opened.</remarks>
        [Test]
        public void ActionDisabledByAnEarlierHandler_IsNotHandledAndDoesNotThrow()
        {
            var screen = new FakeInputContext(false);
            bool isHandled = false;
            screen.Handlers.AddHandler(_back, () => isHandled = true);
            _back.performed += SwitchActionMapAway;
            _stack.Show(screen);

            PressAndRelease(_keyboard.fKey);

            _back.performed -= SwitchActionMapAway;
            Assert.That(isHandled, Is.False);
        }

        [Test]
        public void ContextShownAgain_HandlesEachPressOnce()
        {
            var screen = new FakeInputContext(false);
            int handledCount = 0;
            screen.Handlers.AddHandler(_back, () => handledCount++);
            _stack.Show(screen);
            _stack.Hide(screen);
            _stack.Show(screen);

            PressAndRelease(_keyboard.fKey);

            Assert.That(handledCount, Is.EqualTo(1));
        }

        [Test]
        public void Dispose_StopsRouting()
        {
            var screen = new FakeInputContext(false);
            bool isHandled = false;
            screen.Handlers.AddHandler(_back, () => isHandled = true);
            _stack.Show(screen);

            _router.Dispose();
            PressAndRelease(_keyboard.fKey);

            Assert.That(isHandled, Is.False);
        }

        private void SwitchActionMapAway(InputAction.CallbackContext context)
        {
            _back.actionMap.Disable();
        }
    }
}
