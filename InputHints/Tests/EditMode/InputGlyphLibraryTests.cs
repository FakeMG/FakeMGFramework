using NUnit.Framework;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

namespace FakeMG.InputHints.Tests.EditMode
{
    public sealed class InputGlyphLibraryTests : InputTestFixture
    {
        private InputHintTestFactory _factory;
        private InputGlyphSetSO _playStationSO;
        private InputGlyphSetSO _gamepadSO;
        private InputGlyphLibrarySO _librarySO;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            _factory = new InputHintTestFactory();
            _playStationSO = _factory.CreateGlyphSet(new[] { "DualShockGamepad" });
            _gamepadSO = _factory.CreateGlyphSet(new[] { "Gamepad" });
            _librarySO = _factory.CreateLibrary(_playStationSO, _gamepadSO);
        }

        [TearDown]
        public override void TearDown()
        {
            _factory.DestroyAll();
            base.TearDown();
        }

        [Test]
        public void PlayStationController_UsesTheFirstListedSetThatCoversIt()
        {
            var dualShock = InputSystem.AddDevice<DualShock4GamepadHID>();

            _librarySO.TryFindSetFor(dualShock, out InputGlyphSetSO glyphSetSO);

            Assert.That(glyphSetSO, Is.SameAs(_playStationSO));
        }

        [Test]
        public void UnlistedGamepad_FallsBackToTheBroadGamepadSet()
        {
            var gamepad = InputSystem.AddDevice<Gamepad>();

            _librarySO.TryFindSetFor(gamepad, out InputGlyphSetSO glyphSetSO);

            Assert.That(glyphSetSO, Is.SameAs(_gamepadSO));
        }

        [Test]
        public void DeviceNoSetCovers_IsNotFound()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();

            bool isFound = _librarySO.TryFindSetFor(keyboard, out InputGlyphSetSO glyphSetSO);

            Assert.That(isFound, Is.False);
            Assert.That(glyphSetSO, Is.Null);
        }
    }
}
