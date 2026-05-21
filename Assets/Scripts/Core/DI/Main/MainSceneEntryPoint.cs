using System;
using System.Collections.Generic;
using System.Threading;
using Configs;
using Cysharp.Threading.Tasks;
using UI.Panels;
using UI.Scenes;
using UI.Screens;
using UnityEngine;
using Utils.ResourcesLoader;
using Zenject;

namespace Core.DI.Main
{
    public sealed class MainSceneEntryPoint : IInitializable, IDisposable
    {
        private readonly UiAddressablesConfig _addressablesConfig;
        private readonly SceneUiRoots _sceneUiRoots;
        private readonly IAddressablePrefabLoader _prefabLoader;
        private readonly ScreenPresenterFactory _presenterFactory;

        private readonly Dictionary<ScreenId, LoadedScreenContext> _loadedScreens = new();
        private readonly CancellationTokenSource _lifetimeCts = new();
        private readonly SemaphoreSlim _switchGate = new(1, 1);

        private MainNavigationView _navigationView;
        private ScreenId? _activeScreen;

        public MainSceneEntryPoint(
            UiAddressablesConfig addressablesConfig,
            SceneUiRoots sceneUiRoots,
            IAddressablePrefabLoader prefabLoader,
            ScreenPresenterFactory presenterFactory)
        {
            _addressablesConfig = addressablesConfig;
            _sceneUiRoots = sceneUiRoots;
            _prefabLoader = prefabLoader;
            _presenterFactory = presenterFactory;
        }

        public void Initialize()
        {
            InitializeAsync(_lifetimeCts.Token).Forget();
        }

        public void Dispose()
        {
            _lifetimeCts.Cancel();
            _lifetimeCts.Dispose();
            _switchGate.Dispose();

            if (_navigationView != null)
            {
                _navigationView.TabSelected -= OnTabSelected;
                _prefabLoader.ReleaseInstance(_navigationView.gameObject);
                _navigationView = null;
            }

            foreach (var context in _loadedScreens.Values)
            {
                context.Presenter.Dispose();
                _prefabLoader.ReleaseInstance(context.Instance);
            }

            _loadedScreens.Clear();
        }

        private async UniTaskVoid InitializeAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_addressablesConfig.NavigationPanelKey))
                {
                    Debug.LogError("Navigation panel key is not configured in UiAddressablesConfig.");
                    return;
                }

                var navigationPrefab = await _prefabLoader.InstantiateAsync(
                    _addressablesConfig.NavigationPanelKey,
                    _sceneUiRoots.UiRoot,
                    cancellationToken);

                _navigationView = navigationPrefab.GetComponent<MainNavigationView>();
                if (_navigationView == null)
                {
                    throw new InvalidOperationException("Navigation prefab does not contain MainNavigationView component.");
                }

                _navigationView.TabSelected += OnTabSelected;
                await SwitchToAsync(ScreenId.Clicker, cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void OnTabSelected(ScreenId screenId)
        {
            SwitchToAsync(screenId, _lifetimeCts.Token).Forget();
        }

        private async UniTask SwitchToAsync(ScreenId nextScreen, CancellationToken cancellationToken)
        {
            await _switchGate.WaitAsync(cancellationToken);
            try
            {
                if (_activeScreen == nextScreen)
                {
                    return;
                }

                if (_activeScreen.HasValue && _loadedScreens.TryGetValue(_activeScreen.Value, out var previousContext))
                {
                    previousContext.Presenter.OnHide();
                    previousContext.View.Hide();
                }

                var nextContext = await GetOrLoadScreenAsync(nextScreen, cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                nextContext.View.Show();
                nextContext.Presenter.OnShow();

                _activeScreen = nextScreen;
                _navigationView.SetSelected(nextScreen);
            }
            finally
            {
                _switchGate.Release();
            }
        }

        private async UniTask<LoadedScreenContext> GetOrLoadScreenAsync(ScreenId screenId, CancellationToken cancellationToken)
        {
            if (_loadedScreens.TryGetValue(screenId, out var loaded))
            {
                return loaded;
            }

            if (!_addressablesConfig.TryGetScreenKey(screenId, out var screenKey))
            {
                throw new InvalidOperationException($"Addressable key for screen {screenId} is not configured.");
            }

            var instance = await _prefabLoader.InstantiateAsync(screenKey, _sceneUiRoots.ScreenRoot, cancellationToken);
            var view = instance.GetComponent<ScreenViewBase>();
            if (view == null)
            {
                throw new InvalidOperationException($"Screen prefab for {screenId} does not contain ScreenViewBase.");
            }

            var presenter = _presenterFactory.Create(screenId);
            presenter.Bind(view);
            view.Hide(true);

            var context = new LoadedScreenContext(instance, view, presenter);
            _loadedScreens.Add(screenId, context);
            return context;
        }

        private readonly struct LoadedScreenContext
        {
            public LoadedScreenContext(GameObject instance, ScreenViewBase view, IScreenPresenter presenter)
            {
                Instance = instance;
                View = view;
                Presenter = presenter;
            }

            public GameObject Instance { get; }
            public ScreenViewBase View { get; }
            public IScreenPresenter Presenter { get; }
        }
    }
}
