using UnityEngine.SceneManagement;

namespace FakeMG.SceneLoading
{
    internal static class UnitySceneContextFactory
    {
        public static LoadedSceneContext Create(Scene scene)
        {
            return scene.IsValid()
                ? new LoadedSceneContext(scene.handle, scene.name)
                : default;
        }
    }
}
