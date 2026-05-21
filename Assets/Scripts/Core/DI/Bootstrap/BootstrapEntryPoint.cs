using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using Zenject;

namespace Core.DI.Bootstrap
{
    public sealed class BootstrapEntryPoint : IInitializable
    {
        private const string MainSceneName = "Main";

        public void Initialize()
        {
            LoadMainSceneAsync().Forget();
        }

        private async UniTaskVoid LoadMainSceneAsync()
        {
            var bootstrapScene = SceneManager.GetActiveScene();
            var mainScene = SceneManager.GetSceneByName(MainSceneName);

            if (!mainScene.isLoaded)
            {
                await SceneManager.LoadSceneAsync(MainSceneName, LoadSceneMode.Additive).ToUniTask();
                mainScene = SceneManager.GetSceneByName(MainSceneName);
            }

            if (mainScene.IsValid())
            {
                SceneManager.SetActiveScene(mainScene);
            }

            if (bootstrapScene.IsValid() && bootstrapScene.isLoaded && bootstrapScene.name != MainSceneName)
            {
                var unloadOperation = SceneManager.UnloadSceneAsync(bootstrapScene);
                if (unloadOperation != null)
                {
                    await unloadOperation.ToUniTask();
                }
            }
        }
    }
}
