using UnityEngine;

namespace UI.Scenes
{
    public sealed class SceneUiRoots
    {
        public SceneUiRoots(Transform uiRoot, Transform screenRoot)
        {
            UiRoot = uiRoot;
            ScreenRoot = screenRoot;
        }

        public Transform UiRoot { get; }
        public Transform ScreenRoot { get; }
    }
}
