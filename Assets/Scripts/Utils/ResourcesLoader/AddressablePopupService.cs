using System.Collections.Generic;
using System.Threading;
using Configs;
using Cysharp.Threading.Tasks;
using UI.Popups;
using UI.Scenes;
using UnityEngine;

namespace Utils.ResourcesLoader
{
    public sealed class AddressablePopupService : IPopupService
    {
        private readonly UiAddressablesConfig _uiAddressablesConfig;
        private readonly SceneUiRoots _sceneUiRoots;
        private readonly IAddressablePrefabLoader _prefabLoader;

        private readonly Dictionary<PopupId, LoadedPopupContext> _loaded = new();

        public AddressablePopupService(
            UiAddressablesConfig uiAddressablesConfig,
            SceneUiRoots sceneUiRoots,
            IAddressablePrefabLoader prefabLoader)
        {
            _uiAddressablesConfig = uiAddressablesConfig;
            _sceneUiRoots = sceneUiRoots;
            _prefabLoader = prefabLoader;
        }

        public async UniTask ShowAsync<TPayload>(PopupId popupId, TPayload payload, CancellationToken cancellationToken = default)
        {
            var popup = await GetOrLoadPopupAsync(popupId, cancellationToken);
            if (!popup.IsAlive)
            {
                return;
            }

            if (popup.View is IPopupPayloadReceiver<TPayload> payloadReceiver)
            {
                payloadReceiver.SetPayload(payload);
            }
            else
            {
                Debug.LogError($"Popup {popupId} cannot consume payload of type {typeof(TPayload).Name}.");
                return;
            }

            popup.View.Show();
        }

        public void Hide(PopupId popupId)
        {
            if (TryGetAlivePopup(popupId, out var popup))
            {
                popup.View.Hide();
            }
        }

        public void HideAll()
        {
            if (_loaded.Count == 0)
            {
                return;
            }

            var staleIds = new List<PopupId>();
            foreach (var pair in _loaded)
            {
                var popupId = pair.Key;
                var popup = pair.Value;

                if (!popup.IsAlive)
                {
                    staleIds.Add(popupId);
                    continue;
                }

                popup.View.Hide();
            }

            for (var i = 0; i < staleIds.Count; i++)
            {
                _loaded.Remove(staleIds[i]);
            }
        }

        public void Dispose()
        {
            HideAll();

            foreach (var popup in _loaded.Values)
            {
                if (popup.Instance != null)
                {
                    _prefabLoader.ReleaseInstance(popup.Instance);
                }
            }

            _loaded.Clear();
        }

        private async UniTask<LoadedPopupContext> GetOrLoadPopupAsync(PopupId popupId, CancellationToken cancellationToken)
        {
            if (TryGetAlivePopup(popupId, out var loadedPopup))
            {
                return loadedPopup;
            }

            if (!_uiAddressablesConfig.TryGetPopupKey(popupId, out var popupKey))
            {
                Debug.LogWarning($"Popup key is not configured for popup id {popupId}.");
                return default;
            }

            var popupInstance = await _prefabLoader.InstantiateAsync(popupKey, _sceneUiRoots.UiRoot, cancellationToken);
            var popupView = popupInstance.GetComponent<IPopupView>();
            var popupBehaviour = popupView as MonoBehaviour;
            if (popupView == null)
            {
                Debug.LogError($"Popup prefab for {popupId} does not contain a component implementing {nameof(IPopupView)}.");
                _prefabLoader.ReleaseInstance(popupInstance);
                return default;
            }

            if (popupBehaviour == null)
            {
                Debug.LogError($"Popup view for {popupId} must inherit from {nameof(MonoBehaviour)} to support Unity lifetime checks.");
                _prefabLoader.ReleaseInstance(popupInstance);
                return default;
            }

            popupView.Hide();
            var context = new LoadedPopupContext(popupInstance, popupView, popupBehaviour);
            _loaded[popupId] = context;
            return context;
        }

        private bool TryGetAlivePopup(PopupId popupId, out LoadedPopupContext popup)
        {
            if (_loaded.TryGetValue(popupId, out popup))
            {
                if (popup.IsAlive)
                {
                    return true;
                }

                _loaded.Remove(popupId);
            }

            popup = default;
            return false;
        }

        private readonly struct LoadedPopupContext
        {
            public LoadedPopupContext(GameObject instance, IPopupView view, MonoBehaviour viewBehaviour)
            {
                Instance = instance;
                View = view;
                ViewBehaviour = viewBehaviour;
            }

            public GameObject Instance { get; }
            public IPopupView View { get; }
            public MonoBehaviour ViewBehaviour { get; }
            public bool IsAlive => Instance != null && ViewBehaviour != null;
        }
    }
}
