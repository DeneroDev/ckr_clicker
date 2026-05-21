using System;
using UI.Screens.Breeds;
using UI.Screens.Clicker;
using UI.Screens.Weather;
using Zenject;

namespace UI.Screens
{
    public sealed class ScreenPresenterFactory
    {
        private readonly DiContainer _container;

        public ScreenPresenterFactory(DiContainer container)
        {
            _container = container;
        }

        public IScreenPresenter Create(ScreenId screenId)
        {
            return screenId switch
            {
                ScreenId.Clicker => _container.Instantiate<ClickerScreenPresenter>(),
                ScreenId.Weather => _container.Instantiate<WeatherScreenPresenter>(),
                ScreenId.Breeds => _container.Instantiate<BreedsScreenPresenter>(),
                _ => throw new ArgumentOutOfRangeException(nameof(screenId), screenId, "Unknown screen id")
            };
        }
    }
}
