using System.Collections.Generic;
using FakeMG.InputContexts;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FakeMG.InputHints.Tests.EditMode
{
    /// <summary>Builds the hints, contexts and glyph sets the tests share, and destroys what it created.</summary>
    internal sealed class InputHintTestFactory
    {
        private readonly List<Object> _createdObjects = new();
        private InputActionAsset _actionAsset;

        #region Public Methods

        public InputActionReference CreateAction(string name, params string[] bindingPaths)
        {
            InputAction action = AddAction(name);
            foreach (string bindingPath in bindingPaths)
            {
                action.AddBinding(bindingPath);
            }

            return Reference(action);
        }

        /// <summary>An action for bindings <see cref="CreateAction"/> cannot express, such as composites; pass it to <see cref="Reference"/>.</summary>
        public InputAction AddAction(string name)
        {
            if (!_actionAsset)
            {
                _actionAsset = Track(ScriptableObject.CreateInstance<InputActionAsset>());
                _actionAsset.AddActionMap("Test");
            }

            return _actionAsset.FindActionMap("Test").AddAction(name, InputActionType.Button);
        }

        public InputActionReference Reference(InputAction action)
        {
            return Track(InputActionReference.Create(action));
        }

        public InputGlyphSetSO CreateGlyphSet(string[] deviceLayouts, params (string ControlPath, string SpriteName)[] glyphs)
        {
            var glyphSetSO = Track(ScriptableObject.CreateInstance<InputGlyphSetSO>());
            glyphSetSO.name = string.Join(" + ", deviceLayouts);
            var serializedSet = new SerializedObject(glyphSetSO);
            SerializedProperty layouts = serializedSet.FindProperty("_deviceLayouts");
            layouts.arraySize = deviceLayouts.Length;
            for (int i = 0; i < deviceLayouts.Length; i++)
            {
                layouts.GetArrayElementAtIndex(i).stringValue = deviceLayouts[i];
            }

            SerializedProperty entries = serializedSet.FindProperty("_glyphs");
            entries.arraySize = glyphs.Length;
            for (int i = 0; i < glyphs.Length; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_controlPath").stringValue = glyphs[i].ControlPath;
                entry.FindPropertyRelative("_sprite").objectReferenceValue = CreateSprite(glyphs[i].SpriteName);
            }

            serializedSet.ApplyModifiedPropertiesWithoutUndo();
            return glyphSetSO;
        }

        public InputGlyphLibrarySO CreateLibrary(params InputGlyphSetSO[] glyphSetSOs)
        {
            var librarySO = Track(ScriptableObject.CreateInstance<InputGlyphLibrarySO>());
            var serializedLibrary = new SerializedObject(librarySO);
            SerializedProperty sets = serializedLibrary.FindProperty("_glyphSetSOs");
            sets.arraySize = glyphSetSOs.Length;
            for (int i = 0; i < glyphSetSOs.Length; i++)
            {
                sets.GetArrayElementAtIndex(i).objectReferenceValue = glyphSetSOs[i];
            }

            serializedLibrary.ApplyModifiedPropertiesWithoutUndo();
            return librarySO;
        }

        public void DestroyAll()
        {
            foreach (Object createdObject in _createdObjects)
            {
                Object.DestroyImmediate(createdObject);
            }

            _createdObjects.Clear();
            _actionAsset = null;
        }

        #endregion

        #region Private Methods

        private Sprite CreateSprite(string spriteName)
        {
            Texture2D texture = Track(new Texture2D(4, 4));
            Sprite sprite = Track(Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), Vector2.zero));
            sprite.name = spriteName;
            return sprite;
        }

        private T Track<T>(T createdObject) where T : Object
        {
            _createdObjects.Add(createdObject);
            return createdObject;
        }

        #endregion
    }

    internal sealed class FakeInputContext : IInputContext
    {
        public bool IsModal { get; }
        public InputActionHandlers Handlers { get; } = new();

        public FakeInputContext(bool isModal)
        {
            IsModal = isModal;
        }
    }

    internal sealed class FakeInputHintContext : IInputHintContext
    {
        public IInputContext InputContext { get; }
        public IReadOnlyList<InputHint> Hints { get; }

        public FakeInputHintContext(IInputContext inputContext, params InputHint[] hints)
        {
            InputContext = inputContext;
            Hints = hints;
        }
    }
}
