using FakeMG.ActionMapManagement;
using FakeMG.Settings;
using NSubstitute;
using NUnit.Framework;
using UnityEngine.InputSystem;

namespace FakeMG.KeyBinding.Tests
{
    public sealed class KeyBindingServiceTests : InputTestFixture
    {
        private const int PRIMARY = 0;
        private const int SECONDARY = 1;

        private KeyBindingTestFactory _factory;
        private BindingDeviceFamilySO _familySO;
        private KeyBindingConfigSO _configSO;
        private SettingsStateRepository _settings;
        private KeyBindingService _service;
        private InputAction _jump;
        private InputAction _fire;
        private Keyboard _keyboard;
        private Mouse _mouse;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _mouse = InputSystem.AddDevice<Mouse>();
            _factory = new KeyBindingTestFactory();
            _jump = _factory.AddAction("Jump", "<Keyboard>/space");
            _fire = _factory.AddAction("Fire", "<Keyboard>/f");
            _familySO = _factory.CreateKeyboardMouseFamily("<Keyboard>", "<Mouse>/middleButton");
            _configSO = _factory.CreateConfig(_familySO, _jump, _fire);
            _settings = new SettingsStateRepository();
            _service = new KeyBindingService(_settings, Substitute.For<IActionMapManager>(), _configSO);
            _service.Start();
        }

        [TearDown]
        public override void TearDown()
        {
            _service.Dispose();
            _factory.DestroyAll();
            base.TearDown();
        }

        [Test]
        public void Rebind_KeyPress_BindsTheKey()
        {
            _service.StartRebind(Slot(_jump, SECONDARY));

            Press(_keyboard.kKey);

            Assert.That(_service.IsRebinding, Is.False);
            Assert.That(Slot(_jump, SECONDARY).ControlPath, Is.EqualTo("<Keyboard>/k"));
        }

        [Test]
        public void Rebind_MouseButtonTheFamilyAllows_BindsIt()
        {
            _service.StartRebind(Slot(_jump, SECONDARY));

            Press(_mouse.middleButton);

            Assert.That(Slot(_jump, SECONDARY).ControlPath, Is.EqualTo("<Mouse>/middleButton"));
        }

        /// <remarks>
        /// Regression: the control scheme requires the mouse, and Unity admitted every mouse button because of it, so left and
        /// right clicks were bound although the family does not list them.
        /// </remarks>
        [TestCase("leftButton")]
        [TestCase("rightButton")]
        public void Rebind_MouseButtonOutsideTheFamily_KeepsListening(string buttonName)
        {
            _service.StartRebind(Slot(_jump, SECONDARY));

            Press((UnityEngine.InputSystem.Controls.ButtonControl)_mouse[buttonName]);

            Assert.That(_service.IsRebinding, Is.True);
            Assert.That(Slot(_jump, SECONDARY).IsUnbound, Is.True);
        }

        [Test]
        public void Rebind_AllowedPressAfterARejectedOne_BindsTheAllowedControl()
        {
            _service.StartRebind(Slot(_jump, SECONDARY));
            Press(_mouse.leftButton);

            Press(_keyboard.kKey);

            Assert.That(Slot(_jump, SECONDARY).ControlPath, Is.EqualTo("<Keyboard>/k"));
        }

        [Test]
        public void ResetToDefault_RestoresTheAuthoredKey()
        {
            _jump.ApplyBindingOverride(PRIMARY, "<Keyboard>/j");

            KeyBindingSlot swappedSlot = _service.ResetToDefault(Slot(_jump, PRIMARY));

            Assert.That(Slot(_jump, PRIMARY).ControlPath, Is.EqualTo("<Keyboard>/space"));
            Assert.That(swappedSlot, Is.Null);
        }

        [Test]
        public void ResetToDefault_OnlyResetsThatSlot()
        {
            _jump.ApplyBindingOverride(PRIMARY, "<Keyboard>/j");
            _fire.ApplyBindingOverride(PRIMARY, "<Keyboard>/g");

            _service.ResetToDefault(Slot(_jump, PRIMARY));

            Assert.That(Slot(_fire, PRIMARY).ControlPath, Is.EqualTo("<Keyboard>/g"));
        }

        [Test]
        public void ResetToDefault_DefaultKeyUsedElsewhere_GivesThatSlotTheOldKey()
        {
            _jump.ApplyBindingOverride(PRIMARY, "<Keyboard>/j");
            _fire.ApplyBindingOverride(PRIMARY, "<Keyboard>/space");

            KeyBindingSlot swappedSlot = _service.ResetToDefault(Slot(_jump, PRIMARY));

            Assert.That(swappedSlot, Is.SameAs(Slot(_fire, PRIMARY)));
            Assert.That(Slot(_fire, PRIMARY).ControlPath, Is.EqualTo("<Keyboard>/j"));
        }

        [Test]
        public void ResetToDefault_SavesTheRemainingOverrides()
        {
            _jump.ApplyBindingOverride(PRIMARY, "<Keyboard>/j");
            _fire.ApplyBindingOverride(PRIMARY, "<Keyboard>/g");

            _service.ResetToDefault(Slot(_jump, PRIMARY));

            string savedOverrides = _settings.GetValue(_configSO.KeyBindingOverridesSettingSO);
            Assert.That(savedOverrides, Does.Contain("<Keyboard>/g"));
            Assert.That(savedOverrides, Does.Not.Contain("<Keyboard>/j"));
        }

        [Test]
        public void ResetToDefault_WhileListening_ChangesNothing()
        {
            _jump.ApplyBindingOverride(PRIMARY, "<Keyboard>/j");
            _service.StartRebind(Slot(_fire, SECONDARY));

            KeyBindingSlot swappedSlot = _service.ResetToDefault(Slot(_jump, PRIMARY));

            Assert.That(swappedSlot, Is.Null);
            Assert.That(Slot(_jump, PRIMARY).ControlPath, Is.EqualTo("<Keyboard>/j"));
        }

        private KeyBindingSlot Slot(InputAction action, int slotIndex)
        {
            foreach (KeyBindingSection section in _configSO.KeyBindingCatalogSO.Sections)
            {
                foreach (RebindableAction rebindableAction in section.Actions)
                {
                    if (rebindableAction.Action == action)
                    {
                        return _service.GetSlots(rebindableAction, _familySO)[slotIndex];
                    }
                }
            }

            throw new System.ArgumentException($"'{action.name}' is not in the catalog.", nameof(action));
        }
    }
}
