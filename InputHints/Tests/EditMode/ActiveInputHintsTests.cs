using FakeMG.InputContexts;
using NUnit.Framework;
using UnityEngine.InputSystem;

namespace FakeMG.InputHints.Tests.EditMode
{
    public sealed class ActiveInputHintsTests
    {
        private InputHintTestFactory _factory;
        private InputContextStack _contextStack;
        private ActiveInputHints _activeHints;
        private InputActionReference _back;
        private InputActionReference _delete;

        [SetUp]
        public void SetUp()
        {
            _factory = new InputHintTestFactory();
            _contextStack = new InputContextStack();
            _activeHints = new ActiveInputHints(_contextStack);
            _back = _factory.CreateAction("Back");
            _delete = _factory.CreateAction("Delete");
        }

        [TearDown]
        public void TearDown()
        {
            _activeHints.Dispose();
            _factory.DestroyAll();
        }

        [Test]
        public void Hints_ListNewestContextFirst()
        {
            var screenDelete = new InputHint("Delete", _delete);
            var overlayBack = new InputHint("Back", _back);

            ShowContextWith(false, screenDelete);
            ShowContextWith(false, overlayBack);

            CollectionAssert.AreEqual(new[] { overlayBack, screenDelete }, _activeHints.Hints);
        }

        [Test]
        public void ModalContext_HidesTheHintsOfEveryContextShownBeforeIt()
        {
            var screenDelete = new InputHint("Delete", _delete);
            var dialogBack = new InputHint("Cancel", _back);

            ShowContextWith(false, screenDelete);
            ShowContextWith(true, dialogBack);

            CollectionAssert.AreEqual(new[] { dialogBack }, _activeHints.Hints);
        }

        [Test]
        public void HidingModalContext_RevealsTheHintsBeneathIt()
        {
            var screenDelete = new InputHint("Delete", _delete);
            ShowContextWith(false, screenDelete);
            IInputContext dialog = ShowContextWith(true, new InputHint("Cancel", _back));

            _contextStack.Hide(dialog);

            CollectionAssert.AreEqual(new[] { screenDelete }, _activeHints.Hints);
        }

        [Test]
        public void SharedAction_ShowsOnceFromTheNewestHintUsingIt()
        {
            var screenBack = new InputHint("Back", _back);
            var overlayBack = new InputHint("Close", _back);

            ShowContextWith(false, screenBack);
            ShowContextWith(false, overlayBack);

            CollectionAssert.AreEqual(new[] { overlayBack }, _activeHints.Hints);
        }

        [Test]
        public void HiddenHint_NeitherShowsNorClaimsItsAction()
        {
            var screenBack = new InputHint("Back", _back);
            var overlayBack = new InputHint("Close", _back);
            ShowContextWith(false, screenBack);
            ShowContextWith(false, overlayBack);

            overlayBack.SetShown(false);

            CollectionAssert.AreEqual(new[] { screenBack }, _activeHints.Hints);
        }

        [Test]
        public void HintsOfAContextAddedButNotShown_StayHidden()
        {
            var context = new FakeInputContext(false);

            _activeHints.Add(new FakeInputHintContext(context, new InputHint("Inspect", _back)));

            CollectionAssert.IsEmpty(_activeHints.Hints);
        }

        [Test]
        public void ShowingAContextAddedEarlier_ShowsItsHints()
        {
            var context = new FakeInputContext(false);
            var inspect = new InputHint("Inspect", _back);
            _activeHints.Add(new FakeInputHintContext(context, inspect));

            _contextStack.Show(context);

            CollectionAssert.AreEqual(new[] { inspect }, _activeHints.Hints);
        }

        [Test]
        public void ActiveContextWithoutHints_StillHidesTheHintsBeneathIt()
        {
            ShowContextWith(false, new InputHint("Delete", _delete));

            _contextStack.Show(new FakeInputContext(true));

            CollectionAssert.IsEmpty(_activeHints.Hints);
        }

        [Test]
        public void RelabelingAShownHint_ReportsAChange()
        {
            var delete = new InputHint("Delete world", _delete);
            ShowContextWith(false, delete);
            int changeCount = 0;
            _activeHints.OnChanged += () => changeCount++;

            delete.SetLabel("Delete save");

            Assert.That(changeCount, Is.EqualTo(1));
        }

        [Test]
        public void RelabelingAHintOfARemovedContext_ReportsNothing()
        {
            var delete = new InputHint("Delete world", _delete);
            var hintContext = new FakeInputHintContext(new FakeInputContext(false), delete);
            _activeHints.Add(hintContext);
            _activeHints.Remove(hintContext);
            int changeCount = 0;
            _activeHints.OnChanged += () => changeCount++;

            delete.SetLabel("Delete save");

            Assert.That(changeCount, Is.Zero);
        }

        [Test]
        public void AddingAHintContextTwice_ListsItsHintsOnce()
        {
            var back = new InputHint("Back", _back);
            var context = new FakeInputContext(false);
            var hintContext = new FakeInputHintContext(context, back);
            _contextStack.Show(context);

            _activeHints.Add(hintContext);
            _activeHints.Add(hintContext);

            CollectionAssert.AreEqual(new[] { back }, _activeHints.Hints);
        }

        private IInputContext ShowContextWith(bool isModal, params InputHint[] hints)
        {
            var context = new FakeInputContext(isModal);
            _activeHints.Add(new FakeInputHintContext(context, hints));
            _contextStack.Show(context);
            return context;
        }
    }
}
