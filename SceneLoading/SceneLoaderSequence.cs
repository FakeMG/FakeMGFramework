// using System.Collections.Generic;
// using System.Linq;
// using Cysharp.Threading.Tasks;
// using UnityEngine;
// using UnityEngine.Events;
// using UnityEngine.SceneManagement;
//
// namespace FakeMG.Framework.SceneLoading
// {
//     public class SceneLoaderSequence : MonoBehaviour
//     {
//         [Header("Scene Loaders")]
//         [SerializeField] private List<SceneLoader> sceneLoaders = new();
//
//         [Header("Events")]
//         public UnityEvent onAllScenesLoaded;
//         public UnityEvent onAllScenesUnloaded;
//
//         public bool IsLoading { get; private set; }
//         public bool IsUnloading { get; private set; }
//         public bool IsBusy => IsLoading || IsUnloading;
//         public List<SceneLoader> SceneLoaders => sceneLoaders;
//
//         private void Awake()
//         {
//             ValidateSceneLoaders();
//         }
//
//         private void ValidateSceneLoaders()
//         {
//             var duplicates = sceneLoaders
//                 .Where(loader => loader != null && loader.SceneReference != null)
//                 .GroupBy(loader => loader.SceneReference.AssetGUID)
//                 .Where(group => group.Count() > 1)
//                 .ToList();
//
//             if (duplicates.Any())
//             {
//                 Debug.LogError(
//                     "SceneLoaderSequence: Duplicate scene references found! Each scene can only appear once in the sequence.");
//                 foreach (var duplicate in duplicates)
//                 {
//                     Debug.LogError($"Duplicate scene: {duplicate.First().gameObject.name}");
//                 }
//             }
//         }
//
//         public void LoadScenesSequential()
//         {
//             if (IsBusy)
//             {
//                 Debug.LogWarning("SceneLoaderSequence is busy. Cannot load scenes.");
//                 return;
//             }
//
//             LoadScenesSequentialAsync().Forget();
//         }
//
//         public void LoadScenesParallel()
//         {
//             if (IsBusy)
//             {
//                 Debug.LogWarning("SceneLoaderSequence is busy. Cannot load scenes.");
//                 return;
//             }
//
//             LoadScenesParallelAsync().Forget();
//         }
//
//         public void UnloadAllScenes()
//         {
//             if (IsBusy)
//             {
//                 Debug.LogWarning("SceneLoaderSequence is busy. Cannot unload scenes.");
//                 return;
//             }
//
//             UnloadScenesAsync().Forget();
//         }
//
//         public void ReloadScenesSequential()
//         {
//             if (IsBusy)
//             {
//                 Debug.LogWarning("SceneLoaderSequence is busy. Cannot reload scenes.");
//                 return;
//             }
//
//             ReloadScenesSequentialAsync().Forget();
//         }
//
//         public void ReloadScenesParallel()
//         {
//             if (IsBusy)
//             {
//                 Debug.LogWarning("SceneLoaderSequence is busy. Cannot reload scenes.");
//                 return;
//             }
//
//             ReloadScenesParallelAsync().Forget();
//         }
//
//         public bool AreAllScenesLoaded()
//         {
//             return sceneLoaders
//                 .Where(loader => loader != null)
//                 .All(loader => loader.IsSceneLoaded);
//         }
//
//         public int GetLoadedSceneCount()
//         {
//             return sceneLoaders
//                 .Where(loader => loader != null)
//                 .Count(loader => loader.IsSceneLoaded);
//         }
//
//         private async UniTaskVoid LoadScenesSequentialAsync()
//         {
//             if (sceneLoaders == null || sceneLoaders.Count == 0)
//             {
//                 Debug.LogWarning("No scene loaders configured.");
//                 return;
//             }
//
//             IsLoading = true;
//
//             var validLoaders = sceneLoaders.Where(loader => loader != null).ToList();
//             int successCount = 0;
//             int totalCount = validLoaders.Count;
//
//             foreach (var loader in validLoaders)
//             {
//                 if (loader.IsSceneLoaded)
//                 {
//                     successCount++; // Already loaded
//                     continue; // Skip if already loaded
//                 }
//
//                 bool loadSuccess = await LoadSingleSceneAsync(loader);
//
//                 if (loadSuccess)
//                 {
//                     successCount++;
//                 }
//             }
//
//             IsLoading = false;
//
//             if (successCount == totalCount || AreAllScenesLoaded())
//             {
//                 Debug.Log("Successfully loaded all scenes sequentially.");
//                 onAllScenesLoaded?.Invoke();
//             }
//             else
//             {
//                 Debug.LogWarning($"Loaded {successCount}/{totalCount} scenes successfully.");
//             }
//         }
//
//         private async UniTaskVoid LoadScenesParallelAsync()
//         {
//             if (sceneLoaders == null || sceneLoaders.Count == 0)
//             {
//                 Debug.LogWarning("No scene loaders configured.");
//                 return;
//             }
//
//             IsLoading = true;
//
//             var validLoaders = sceneLoaders.Where(loader => loader != null).ToList();
//             int successCount = 0;
//             int totalCount = validLoaders.Count;
//
//             List<UniTask<bool>> loadTasks = new();
//
//             foreach (var loader in validLoaders)
//             {
//                 if (!loader.IsSceneLoaded)
//                 {
//                     loadTasks.Add(LoadSingleSceneAsync(loader));
//                 }
//                 else
//                 {
//                     successCount++; // Already loaded
//                 }
//             }
//
//             var results = await UniTask.WhenAll(loadTasks);
//
//             successCount += results.Count(result => result);
//
//             IsLoading = false;
//
//             if (successCount == totalCount || AreAllScenesLoaded())
//             {
//                 Debug.Log("Successfully loaded all scenes in parallel.");
//                 onAllScenesLoaded?.Invoke();
//             }
//             else
//             {
//                 Debug.LogWarning($"Loaded {successCount}/{totalCount} scenes successfully.");
//             }
//         }
//
//         private async UniTask<bool> LoadSingleSceneAsync(SceneLoader loader)
//         {
//             bool wasLoaded = loader.IsSceneLoaded;
//
//             await loader.LoadSceneAsync(LoadSceneMode.Additive);
//
//             // Return true if the scene is now loaded and wasn't loaded before
//             return loader.IsSceneLoaded && !wasLoaded;
//         }
//
//         private async UniTaskVoid UnloadScenesAsync()
//         {
//             if (sceneLoaders == null || sceneLoaders.Count == 0)
//             {
//                 Debug.LogWarning("No scene loaders configured.");
//                 return;
//             }
//
//             IsUnloading = true;
//
//             var validLoaders = sceneLoaders.Where(loader => loader != null && loader.IsSceneLoaded).ToList();
//             int successCount = 0;
//             int totalCount = validLoaders.Count;
//
//             // Unload all scenes in parallel for better performance
//             List<UniTask<bool>> unloadTasks = new();
//
//             foreach (var loader in validLoaders)
//             {
//                 unloadTasks.Add(UnloadSingleSceneAsync(loader));
//             }
//
//             var results = await UniTask.WhenAll(unloadTasks);
//
//             successCount = results.Count(result => result);
//
//             IsUnloading = false;
//
//             if (successCount == totalCount || GetLoadedSceneCount() == 0)
//             {
//                 Debug.Log("Successfully unloaded all scenes in sequence.");
//                 onAllScenesUnloaded?.Invoke();
//             }
//             else
//             {
//                 Debug.LogWarning($"Unloaded {successCount}/{totalCount} scenes successfully.");
//             }
//         }
//
//         private async UniTask<bool> UnloadSingleSceneAsync(SceneLoader loader)
//         {
//             bool wasLoaded = loader.IsSceneLoaded;
//
//             await loader.UnloadSceneAsync();
//
//             // Return true if the scene was loaded before and is now unloaded
//             return wasLoaded && !loader.IsSceneLoaded;
//         }
//
//         private async UniTaskVoid ReloadScenesSequentialAsync()
//         {
//             if (sceneLoaders == null || sceneLoaders.Count == 0)
//             {
//                 Debug.LogWarning("No scene loaders configured.");
//                 return;
//             }
//
//             IsLoading = true;
//
//             var validLoaders = sceneLoaders.Where(loader => loader != null).ToList();
//             int successCount = 0;
//             int totalCount = validLoaders.Count;
//
//             foreach (var loader in validLoaders)
//             {
//                 if (!loader.IsSceneLoaded)
//                 {
//                     Debug.LogWarning(
//                         $"Scene {loader.gameObject.name} is not loaded. Skipping reload.");
//                     continue;
//                 }
//
//                 bool reloadSuccess = await ReloadSingleSceneAsync(loader);
//
//                 if (reloadSuccess)
//                 {
//                     successCount++;
//                 }
//             }
//
//             IsLoading = false;
//
//             if (successCount > 0)
//             {
//                 Debug.Log($"Successfully reloaded {successCount} scenes sequentially.");
//                 onAllScenesLoaded?.Invoke();
//             }
//             else
//             {
//                 Debug.LogWarning("No scenes were reloaded.");
//             }
//         }
//
//         private async UniTaskVoid ReloadScenesParallelAsync()
//         {
//             if (sceneLoaders == null || sceneLoaders.Count == 0)
//             {
//                 Debug.LogWarning("No scene loaders configured.");
//                 return;
//             }
//
//             IsLoading = true;
//
//             var validLoaders = sceneLoaders.Where(loader => loader != null && loader.IsSceneLoaded).ToList();
//             int totalCount = validLoaders.Count;
//
//             if (totalCount == 0)
//             {
//                 Debug.LogWarning("No loaded scenes to reload.");
//                 IsLoading = false;
//                 return;
//             }
//
//             List<UniTask<bool>> reloadTasks = new();
//
//             foreach (var loader in validLoaders)
//             {
//                 reloadTasks.Add(ReloadSingleSceneAsync(loader));
//             }
//
//             var results = await UniTask.WhenAll(reloadTasks);
//
//             int successCount = results.Count(result => result);
//
//             IsLoading = false;
//
//             if (successCount > 0)
//             {
//                 Debug.Log($"Successfully reloaded {successCount} scenes in parallel.");
//                 onAllScenesLoaded?.Invoke();
//             }
//             else
//             {
//                 Debug.LogWarning("No scenes were reloaded successfully.");
//             }
//         }
//
//         private async UniTask<bool> ReloadSingleSceneAsync(SceneLoader loader)
//         {
//             bool wasLoaded = loader.IsSceneLoaded;
//
//             if (!wasLoaded)
//             {
//                 return false;
//             }
//
//             await loader.ReloadSceneAsync();
//
//             // Return true if the scene is loaded after reload
//             return loader.IsSceneLoaded;
//         }
//
//         private void OnDestroy()
//         {
//             // Clean up - unload all scenes
//             if (sceneLoaders != null)
//             {
//                 foreach (var loader in sceneLoaders.Where(l => l != null && l.IsSceneLoaded))
//                 {
//                     loader.UnloadSceneAsync().Forget();
//                 }
//             }
//         }
//     }
// }

