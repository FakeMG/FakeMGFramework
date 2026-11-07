using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FakeMG.InputContexts.Tests.EditMode
{
    /// <summary>Builds the actions the tests share, inside one action asset as the Input System expects, and destroys them.</summary>
    internal sealed class InputContextTestFactory
    {
        private readonly List<Object> _createdObjects = new();
        private InputActionAsset _actionAsset;

        #region Public Methods

        public InputAction CreateAction(string name, params string[] bindingPaths)
        {
            if (!_actionAsset)
            {
                _actionAsset = ScriptableObject.CreateInstance<InputActionAsset>();
                _actionAsset.AddActionMap("Test");
                _createdObjects.Add(_actionAsset);
            }

            InputAction action = _actionAsset.FindActionMap("Test").AddAction(name, InputActionType.Button);
            foreach (string bindingPath in bindingPaths)
            {
                action.AddBinding(bindingPath);
            }

            return action;
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
}
