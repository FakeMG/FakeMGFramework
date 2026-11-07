using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace FakeMG.InputHints.Tests.EditMode
{
    public sealed class InputGlyphResolverTests : InputTestFixture
    {
        private static readonly string[] KEYBOARD_MOUSE = { "Keyboard", "Mouse" };
        private static readonly string[] GAMEPAD = { "Gamepad" };

        private InputHintTestFactory _factory;
        private InputGlyphResolver _resolver;
        private List<InputGlyph> _glyphs;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            InputSystem.AddDevice<Gamepad>();
            _factory = new InputHintTestFactory();
            _resolver = new InputGlyphResolver();
            _glyphs = new List<InputGlyph>();
        }

        [TearDown]
        public override void TearDown()
        {
            _factory.DestroyAll();
            base.TearDown();
        }

        [Test]
        public void KeyBinding_IsDrawnWithItsIcon()
        {
            InputGlyphSetSO keyboardSO = _factory.CreateGlyphSet(KEYBOARD_MOUSE, ("f", "keyboard_f"));
            var hint = new InputHint("Back", _factory.CreateAction("Back", "<Keyboard>/f"));

            _resolver.Resolve(hint, keyboardSO, _glyphs);

            CollectionAssert.AreEqual(new[] { "keyboard_f" }, SpriteNames());
        }

        [Test]
        public void SeveralUsableBindings_ShowOnlyTheFirst()
        {
            InputGlyphSetSO keyboardSO = _factory.CreateGlyphSet(KEYBOARD_MOUSE, ("f", "keyboard_f"), ("escape", "keyboard_escape"));
            var hint = new InputHint("Back", _factory.CreateAction("Back", "<Keyboard>/f", "<Keyboard>/escape"));

            _resolver.Resolve(hint, keyboardSO, _glyphs);

            CollectionAssert.AreEqual(new[] { "keyboard_f" }, SpriteNames());
        }

        [Test]
        public void GlyphSet_ShowsTheBindingOfItsOwnDevices()
        {
            InputGlyphSetSO gamepadSO = _factory.CreateGlyphSet(GAMEPAD, ("buttonSouth", "xbox_button_a"));
            var hint = new InputHint("Load", _factory.CreateAction("Load", "<Keyboard>/enter", "<Gamepad>/buttonSouth"));

            _resolver.Resolve(hint, gamepadSO, _glyphs);

            CollectionAssert.AreEqual(new[] { "xbox_button_a" }, SpriteNames());
        }

        [Test]
        public void ActionTheDevicesCannotPerform_ShowsNothing()
        {
            InputGlyphSetSO gamepadSO = _factory.CreateGlyphSet(GAMEPAD, ("buttonSouth", "xbox_button_a"));
            var hint = new InputHint("Inspect", _factory.CreateAction("Inspect", "<Mouse>/rightButton"));

            _resolver.Resolve(hint, gamepadSO, _glyphs);

            Assert.That(_glyphs, Is.Empty);
        }

        [Test]
        public void DirectionalComposite_ReadsUpLeftDownRight()
        {
            InputGlyphSetSO keyboardSO = _factory.CreateGlyphSet(
                KEYBOARD_MOUSE, ("w", "keyboard_w"), ("a", "keyboard_a"), ("s", "keyboard_s"), ("d", "keyboard_d"));
            var move = _factory.AddAction("Move");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            var hint = new InputHint("Move", _factory.Reference(move));

            _resolver.Resolve(hint, keyboardSO, _glyphs);

            CollectionAssert.AreEqual(new[] { "keyboard_w", "keyboard_a", "keyboard_s", "keyboard_d" }, SpriteNames());
        }

        [Test]
        public void CompositePartBoundTwice_ShowsItsFirstBindingOnly()
        {
            InputGlyphSetSO keyboardSO = _factory.CreateGlyphSet(
                KEYBOARD_MOUSE, ("w", "keyboard_w"), ("upArrow", "keyboard_arrow_up"), ("s", "keyboard_s"));
            var move = _factory.AddAction("Move");
            move.AddCompositeBinding("1DAxis")
                .With("Positive", "<Keyboard>/w")
                .With("Positive", "<Keyboard>/upArrow")
                .With("Negative", "<Keyboard>/s");
            var hint = new InputHint("Move", _factory.Reference(move));

            _resolver.Resolve(hint, keyboardSO, _glyphs);

            CollectionAssert.AreEqual(new[] { "keyboard_w", "keyboard_s" }, SpriteNames());
        }

        [Test]
        public void ModifierComposite_KeepsModifierBeforeItsButton()
        {
            InputGlyphSetSO keyboardSO = _factory.CreateGlyphSet(KEYBOARD_MOUSE, ("leftShift", "keyboard_shift"), ("leftButton", "mouse_left"));
            var pickOne = _factory.AddAction("Pick One");
            pickOne.AddCompositeBinding("OneModifier")
                .With("Modifier", "<Keyboard>/leftShift")
                .With("Binding", "<Mouse>/leftButton");
            var hint = new InputHint("Take one", _factory.Reference(pickOne));

            _resolver.Resolve(hint, keyboardSO, _glyphs);

            CollectionAssert.AreEqual(new[] { "keyboard_shift", "mouse_left" }, SpriteNames());
        }

        [Test]
        public void CompositeWithAPartTheDevicesLack_FallsThroughToTheNextBinding()
        {
            InputGlyphSetSO gamepadSO = _factory.CreateGlyphSet(GAMEPAD, ("leftStick", "xbox_stick_l"));
            var move = _factory.AddAction("Move");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s");
            move.AddBinding("<Gamepad>/leftStick");
            var hint = new InputHint("Move", _factory.Reference(move));

            _resolver.Resolve(hint, gamepadSO, _glyphs);

            CollectionAssert.AreEqual(new[] { "xbox_stick_l" }, SpriteNames());
        }

        [Test]
        public void HintWithSeveralActions_ShowsEachActionsGlyph()
        {
            InputGlyphSetSO keyboardSO = _factory.CreateGlyphSet(KEYBOARD_MOUSE, ("q", "keyboard_q"), ("e", "keyboard_e"));
            var hint = new InputHint(
                "Rotate",
                _factory.CreateAction("Rotate Left", "<Keyboard>/q"),
                _factory.CreateAction("Rotate Right", "<Keyboard>/e"));

            _resolver.Resolve(hint, keyboardSO, _glyphs);

            CollectionAssert.AreEqual(new[] { "keyboard_q", "keyboard_e" }, SpriteNames());
        }

        [Test]
        public void MissingIcon_ShowsTheControlNameAndWarns()
        {
            InputGlyphSetSO keyboardSO = _factory.CreateGlyphSet(KEYBOARD_MOUSE);
            var hint = new InputHint("Pause", _factory.CreateAction("Pause", "<Keyboard>/space"));
            LogAssert.Expect(LogType.Warning, new Regex("no glyph for 'space'"));

            _resolver.Resolve(hint, keyboardSO, _glyphs);

            Assert.That(_glyphs.Single().HasSprite, Is.False);
            Assert.That(_glyphs.Single().ControlName, Is.Not.Empty);
        }

        [Test]
        public void LetterKey_IsDrawnByTheCharacterItPrintsOnTheActiveLayout()
        {
            SetKeyInfo(Key.Q, "A");
            InputGlyphSetSO keyboardSO = _factory.CreateGlyphSet(KEYBOARD_MOUSE, ("q", "keyboard_q"), ("a", "keyboard_a"));
            var hint = new InputHint("Previous", _factory.CreateAction("Previous", "<Keyboard>/q"));

            _resolver.Resolve(hint, keyboardSO, _glyphs);

            CollectionAssert.AreEqual(new[] { "keyboard_a" }, SpriteNames());
        }

        private string[] SpriteNames()
        {
            return _glyphs.Select(glyph => glyph.Sprite ? glyph.Sprite.name : null).ToArray();
        }
    }
}
