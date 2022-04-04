using System.Threading.Tasks;

using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

using Alteracia.Patterns;

namespace Alteracia.Screenplay
{
    public static class SceneLoadingUtils
    {
        public static void RemoveScene(string id)
        {
            SceneManager.UnloadSceneAsync(id);
            // TODO test case: scene not loaded yet
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
            
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(id));
        }
    }
    
    public abstract class SceneLoader
    {
        protected string id;
        public string Id => id;

        protected SceneLoader(string sceneName)
        {
            id = sceneName;
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
        public AddressableSceneLoader(string sceneName) : base(sceneName) { }

        public override async Task LoadScene()
        {
            var sceneAsyncOperation = Addressables.LoadSceneAsync(id);
            await sceneAsyncOperation.Task;
        }

        public override async Task AddScene()
        {
            var sceneAsyncOperation = Addressables.LoadSceneAsync(id, LoadSceneMode.Additive);
            await sceneAsyncOperation.Task;
        }
    }

    public class BuildInSceneLoader : SceneLoader
    {
        public BuildInSceneLoader(string sceneName) : base(sceneName) { }

        public override async Task LoadScene()
        {
            var sceneAsyncOperation = SceneManager.LoadSceneAsync(id);
            await AltTasks.WaitWhile(() => !sceneAsyncOperation.isDone);
        }

        public override async Task AddScene()
        {
            var sceneAsyncOperation = SceneManager.LoadSceneAsync(id, LoadSceneMode.Additive);
            await AltTasks.WaitWhile(() => !sceneAsyncOperation.isDone);
        }
    }

}