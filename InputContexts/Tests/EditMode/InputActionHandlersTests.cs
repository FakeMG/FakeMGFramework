using NUnit.Framework;
using UnityEngine.InputSystem;

namespace FakeMG.InputContexts.Tests.EditMode
{
    public sealed class InputActionHandlersTests
    {
        private InputContextTestFactory _factory;
        private InputActionHandlers _handlers;
        private InputAction _back;
        private int _handledCount;

        [SetUp]
        public void SetUp()
        {
            _factory = new InputContextTestFactory();
            _handlers = new InputActionHandlers();
            _back = _factory.CreateAction("Back");
            _handledCount = 0;
        }

        [TearDown]
        public void TearDown()
        {
            _factory.DestroyAll();
        }

        [Test]
        public void Handle_RunsEveryHandlerOfTheAction()
        {
            _handlers.AddHandler(_back, CountHandled);
            _handlers.AddHandler(_back, CountHandled);

            _handlers.Handle(_back);

            Assert.That(_handledCount, Is.EqualTo(2));
        }

        [Test]
        public void RemovingTheLastHandler_StopsAnsweringTheAction()
        {
            _handlers.AddHandler(_back, CountHandled);

            _handlers.RemoveHandler(_back, CountHandled);

            Assert.That(_handlers.Handles(_back), Is.False);
            CollectionAssert.IsEmpty(_handlers.Actions);
        }

        [Test]
        public void HandlingAnUnansweredAction_DoesNothing()
        {
            Assert.DoesNotThrow(HandleBack);
        }

        [Test]
        public void RemovingAHandlerNeverAdded_ReportsNoChange()
        {
            int changeCount = 0;
            _handlers.OnChanged += () => changeCount++;

            _handlers.RemoveHandler(_back, CountHandled);

            Assert.That(changeCount, Is.Zero);
        }

        private void CountHandled()
        {
            _handledCount++;
        }

        private void HandleBack()
        {
            _handlers.Handle(_back);
        }
    }
}
