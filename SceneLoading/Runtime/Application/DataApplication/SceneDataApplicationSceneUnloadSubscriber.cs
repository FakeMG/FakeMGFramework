using System;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace FakeMG.SceneLoading
{
    internal sealed class SceneDataApplicationSceneUnloadSubscriber : IInitializable, IDisposable
    {
        private readonly ISceneDataApplicationSceneLifecycle _sceneLifecycle;

        public SceneDataApplicationSceneUnloadSubscriber(ISceneDataApplicationSceneLifecycle sceneLifecycle)
        {
            _sceneLifecycle = sceneLifecycle;
        }

        #region Public Methods

        public void Initialize()
        {
            SceneManager.sceneUnloaded += RemoveUnloadedScene;
        }

        public void Dispose()
        {
            SceneManager.sceneUnloaded -= RemoveUnloadedScene;
        }

        #endregion

        #region Private Methods

        private void RemoveUnloadedScene(Scene unloadedScene)
        {
            _sceneLifecycle.RemoveScene(UnitySceneContextFactory.Create(unloadedScene));
        }

        #endregion
    }
}
