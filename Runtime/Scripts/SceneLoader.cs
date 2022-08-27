using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

using Alteracia.Patterns;

namespace Alteracia.Screenplay
{
    public static class SceneLoadingUtils
    {
        private static readonly List<string> Loading = new List<string>();
        
        public static bool AddLoadingScene(string sceneName)
        {
            if (IsLoad(sceneName)) return false;
            Loading.Add(sceneName);
            return true;
        }
        
        public static bool IsLoad(string sceneName) => Loading.Contains(sceneName);
        
        public static void RemoveScene(string id)
        {
            if (!IsLoad(id) || 
                !SceneManager.GetSceneByName(id).isLoaded) // Case when scene was not loaded yet
                return;
            
            SceneManager.UnloadSceneAsync(id);
            Loading.Remove(id);
        }
        
        public static bool IsSceneRemote(string id)
        {
            for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                if (scenePath.EndsWith(id + ".unity")) return false;
            }

            return true;
        }
        
        public static void ActivateScene(string id)
        {
            if (string.IsNullOrEmpty(id) || id == SceneManager.GetActiveScene().name) return;

            var scene = SceneManager.GetSceneByName(id);
            if(scene.isLoaded == false)
                return;
            SceneManager.SetActiveScene(scene);
        }
    }
    
    public abstract class SceneLoader
    {
        protected readonly Action<float> onLoadingProgress;
        
        protected string id;
        public string Id => id;

        protected SceneLoader(string sceneName, Action<float> loadingProgress)
        {
            id = sceneName;
            this.onLoadingProgress = loadingProgress;
        }

        public abstract Task LoadScene();
        public abstract Task AddScene();

        public async Task ReloadScene()
        {
            SceneLoadingUtils.RemoveScene(id);
            await AltTasks.WaitFrames(1);
            await AddScene();
        }
    }

    public class AddressableSceneLoader : SceneLoader
    {
        public AddressableSceneLoader(string sceneName, Action<float> progress) : base(sceneName, progress) { }

        public override async Task LoadScene()
        {
            if (!SceneLoadingUtils.AddLoadingScene(id)) return;

            var sceneAsyncOperation = Addressables.LoadSceneAsync(id);
            while (!sceneAsyncOperation.IsDone)
            {
                onLoadingProgress?.Invoke(sceneAsyncOperation.PercentComplete);
                await Task.Yield();
            }
            onLoadingProgress?.Invoke(1f);
        }

        public override async Task AddScene()
        {
            if (!SceneLoadingUtils.AddLoadingScene(id)) return;
            
            var sceneAsyncOperation = Addressables.LoadSceneAsync(id, LoadSceneMode.Additive);
            while (!sceneAsyncOperation.IsDone)
            {
                onLoadingProgress?.Invoke(sceneAsyncOperation.PercentComplete);
                await Task.Yield();
            }
            onLoadingProgress?.Invoke(1f);
            //await sceneAsyncOperation.Task;
        }
    }

    public class BuildInSceneLoader : SceneLoader
    {
        public BuildInSceneLoader(string sceneName, Action<float> progress) : base(sceneName, progress) { }

        public override async Task LoadScene()
        {
            if (!SceneLoadingUtils.AddLoadingScene(id)) return;
            
            var sceneAsyncOperation = SceneManager.LoadSceneAsync(id);
            while (!sceneAsyncOperation.isDone)
            {
                onLoadingProgress?.Invoke(sceneAsyncOperation.progress);
                await Task.Yield();
            }
            onLoadingProgress?.Invoke(1f);
            // await AltTasks.WaitWhile(() => !sceneAsyncOperation.isDone);
        }

        public override async Task AddScene()
        {
            if (!SceneLoadingUtils.AddLoadingScene(id)) return;
            
            var sceneAsyncOperation = SceneManager.LoadSceneAsync(id, LoadSceneMode.Additive);
            while (!sceneAsyncOperation.isDone)
            {
                onLoadingProgress?.Invoke(sceneAsyncOperation.progress);
                await Task.Yield();
            }
            onLoadingProgress?.Invoke(1f);
            //await AltTasks.WaitWhile(() => !sceneAsyncOperation.isDone);
        }
    }

}