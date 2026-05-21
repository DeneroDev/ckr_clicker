using Configs;
using Core.Breeds;
using Core.Weather;
using UI.Scenes;
using UI.Screens;
using UnityEngine;
using Utils.Particles;
using Utils.Requests;
using Utils.ResourcesLoader;
using Zenject;

namespace Core.DI.Main
{
    public sealed class MainSceneInstaller : MonoInstaller
    {
        [Header("Addressables")]
        [SerializeField] private UiAddressablesConfig _uiAddressablesConfig;

        [Header("Scene roots")]
        [SerializeField] private Transform _uiRoot;
        [SerializeField] private Transform _screenRoot;

        [Header("Feature configs")]
        [SerializeField] private ClickerBalanceConfig _clickerBalanceConfig;

        public override void InstallBindings()
        {
            Container.BindInstance(_uiAddressablesConfig).AsSingle();
            Container.BindInstance(_clickerBalanceConfig).AsSingle();
            Container.BindInstance(new SceneUiRoots(_uiRoot, _screenRoot)).AsSingle();

            Container.BindInterfacesAndSelfTo<RequestQueue>().AsSingle().IfNotBound();
            Container.BindInterfacesAndSelfTo<AddressablePrefabLoader>().AsSingle();
            Container.BindInterfacesAndSelfTo<AddressablePopupService>().AsSingle();
            Container.BindInterfacesAndSelfTo<ParticlePoolService>().AsSingle();
            Container.BindInterfacesAndSelfTo<BreedsDataLoader>().AsSingle();
            Container.BindInterfacesAndSelfTo<WeatherDataLoader>().AsSingle();
            Container.BindInterfacesAndSelfTo<ScreenPresenterFactory>().AsSingle();
            Container.BindInterfacesTo<MainSceneEntryPoint>().AsSingle();
        }
    }
}
