using NUnit.Framework;
using UnityEngine.InputSystem;

namespace FakeMG.InputHints.Tests.EditMode
{
    public sealed class ActiveGlyphSetTrackerTests : InputTestFixture
    {
        private InputHintTestFactory _factory;
        private InputGlyphSetSO _playStationSO;
        private InputGlyphSetSO _keyboardSO;
        private InputGlyphSetSO _gamepadSO;
        private ActiveGlyphSetTracker _tracker;
        private Keyboard _keyboard;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _factory = new InputHintTestFactory();
            _playStationSO = _factory.CreateGlyphSet(new[] { "DualShockGamepad" });
            _keyboardSO = _factory.CreateGlyphSet(new[] { "Keyboard", "Mouse" });
            _gamepadSO = _factory.CreateGlyphSet(new[] { "Gamepad" });
            _tracker = new ActiveGlyphSetTracker(_factory.CreateLibrary(_playStationSO, _keyboardSO, _gamepadSO));
            _tracker.Initialize();
        }

        [TearDown]
        public override void TearDown()
        {
            _tracker.Dispose();
            _factory.DestroyAll();
            base.TearDown();
        }

        [Test]
        public void Initialize_PicksTheSetOfAConnectedDevice()
        {
            Assert.That(_tracker.ActiveSetSO, Is.SameAs(_keyboardSO));
        }

        [Test]
        public void PressingAGamepadButton_SwitchesToTheGamepadSet()
        {
            var gamepad = InputSystem.AddDevice<Gamepad>();
            int changeCount = 0;
            _tracker.OnGlyphsChanged += () => changeCount++;

            Press(gamepad.buttonSouth);

            Assert.That(_tracker.ActiveSetSO, Is.SameAs(_gamepadSO));
            Assert.That(changeCount, Is.EqualTo(1));
        }

        [Test]
        public void PressingAnotherDeviceOfTheActiveSet_ReportsNoChange()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            int changeCount = 0;
            _tracker.OnGlyphsChanged += () => changeCount++;

            Press(mouse.leftButton);
            Press(_keyboard.fKey);

            Assert.That(changeCount, Is.Zero);
        }

        [Test]
        public void PluggingInADeviceTheActiveSetDraws_ReportsAGlyphChange()
        {
            int changeCount = 0;
            _tracker.OnGlyphsChanged += () => changeCount++;

            InputSystem.AddDevice<Mouse>();

            Assert.That(_tracker.ActiveSetSO, Is.SameAs(_keyboardSO));
            Assert.That(changeCount, Is.EqualTo(1));
        }

        [Test]
        public void PluggingInADeviceOfAnotherSet_ReportsNoChange()
        {
            int changeCount = 0;
            _tracker.OnGlyphsChanged += () => changeCount++;

            InputSystem.AddDevice<Gamepad>();

            Assert.That(changeCount, Is.Zero);
        }

        [Test]
        public void UnpluggingTheFollowedGamepad_FallsBackToAConnectedDevice()
        {
            var gamepad = InputSystem.AddDevice<Gamepad>();
            Press(gamepad.buttonSouth);

            InputSystem.RemoveDevice(gamepad);

            Assert.That(_tracker.ActiveSetSO, Is.SameAs(_keyboardSO));
        }

        [Test]
        public void Dispose_StopsFollowingPresses()
        {
            var gamepad = InputSystem.AddDevice<Gamepad>();
            _tracker.Dispose();

            Press(gamepad.buttonSouth);

            Assert.That(_tracker.ActiveSetSO, Is.SameAs(_keyboardSO));
        }
    }
}
