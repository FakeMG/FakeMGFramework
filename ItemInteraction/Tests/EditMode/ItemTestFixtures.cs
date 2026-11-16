using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FakeMG.ItemInteraction.Tests
{
    public sealed class ItemTestFixtures : IDisposable
    {
        private readonly List<Object> _createdObjects = new();

        public AnimationClip HoldingClip { get; }

        public AnimationClip UseClip { get; }

        public ItemAnimationProfileSO ProfileSO { get; }

        public ItemTestFixtures()
        {
            HoldingClip = CreateClip();
            UseClip = CreateClip();
            ProfileSO = Track(ScriptableObject.CreateInstance<ItemAnimationProfileSO>());
            ProfileSO.ConfigureAnimationsForEditor(HoldingClip, UseClip);
        }

        #region Public Methods

        public AnimationClip CreateClip()
        {
            return Track(new AnimationClip());
        }

        public ItemActionSO CreateAction(ItemInputLifecycleSO inputLifecycleSO = null)
        {
            var actionSO = Track(ScriptableObject.CreateInstance<ItemActionSO>());
            actionSO.ConfigureForEditor(inputLifecycleSO, null, ProfileSO, null);
            return actionSO;
        }

        /// <summary>
        /// Destroys the object on Dispose, for test assets this helper does not create itself.
        /// </summary>
        public TObject Track<TObject>(TObject createdObject) where TObject : Object
        {
            _createdObjects.Add(createdObject);
            return createdObject;
        }

        public void Dispose()
        {
            foreach (Object createdObject in _createdObjects)
            {
                Object.DestroyImmediate(createdObject);
            }

            _createdObjects.Clear();
        }

        #endregion
    }
}
