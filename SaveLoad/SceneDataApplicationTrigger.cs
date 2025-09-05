using Cysharp.Threading.Tasks;
using FakeMG.Framework.SaveLoad.Advanced;
using FakeMG.Framework.SceneLoading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace FakeMG.Framework.SaveLoad
{
    /// <summary>
    /// Integrates with SceneLoader to automatically trigger the data application
    /// after a scene is loaded. Attach this to the same GameObject as SceneLoader.
    /// </summary>
    [RequireComponent(typeof(SceneLoader))]
    public class SceneDataApplicationTrigger : MonoBehaviour
    {
        [SerializeField] private SceneLoader sceneLoader;

        [Header("Events")]
        public UnityEvent onDataApplicationStarted;
        public UnityEvent onDataApplicationCompleted;
        public UnityEvent<string> onDataApplicationFailed;

        private void OnEnable()
        {
            sceneLoader.onSceneLoaded.AddListener(OnSceneLoaded);
        }

        private void OnDisable()
        {
            sceneLoader.onSceneLoaded.RemoveListener(OnSceneLoaded);
        }

        private void OnSceneLoaded()
        {
            TriggerDataApplicationAsync().Forget();
        }

        private async UniTaskVoid TriggerDataApplicationAsync()
        {
            if (!sceneLoader.IsSceneLoaded)
            {
                Debug.LogWarning("[SceneDataApplicationTrigger] Scene not loaded, skipping data application");
                return;
            }

            // Get the loaded scene name
            string sceneName = GetLoadedSceneName();
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[SceneDataApplicationTrigger] Could not determine loaded scene name");
                onDataApplicationFailed?.Invoke("Could not determine scene name");
                return;
            }

            // Trigger data application
            onDataApplicationStarted?.Invoke();

            bool success = await DataApplicationManager.Instance.ApplyDataForSceneAsync(sceneName);

            if (success)
            {
                Debug.Log(
                    $"[SceneDataApplicationTrigger] Data application completed successfully for scene {sceneName}");
                onDataApplicationCompleted?.Invoke();
            }
            else
            {
                Debug.LogWarning(
                    $"[SceneDataApplicationTrigger] Data application completed with errors for scene {sceneName}");
                onDataApplicationFailed?.Invoke($"Data application failed for scene {sceneName}");
            }
        }

        private string GetLoadedSceneName()
        {
            if (sceneLoader?.SceneReference == null) return null;

            // Try to get the scene name from the loaded scene
            var handle = sceneLoader.SceneReference.OperationHandle;
            if (handle.IsValid())
            {
                try
                {
                    if (handle.Result is SceneInstance sceneInstance)
                    {
                        return sceneInstance.Scene.name;
                    }
                }
                catch
                {
                    // Fallback to asset reference name
                }
            }

            // Try to get the scene name from SubObjectName or Asset path
            if (!string.IsNullOrEmpty(sceneLoader.SceneReference.SubObjectName))
            {
                return sceneLoader.SceneReference.SubObjectName;
            }

            // Last resort: check an active scene
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.isLoaded)
            {
                return activeScene.name;
            }

            return null;
        }
    }
}