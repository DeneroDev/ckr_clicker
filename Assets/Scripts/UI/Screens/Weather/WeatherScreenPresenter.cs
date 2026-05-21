using System;
using System.Threading;
using Core.Weather;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UI.Screens.Weather
{
    public sealed class WeatherScreenPresenter : ScreenPresenterBase<WeatherScreenView>
    {
        private readonly IWeatherDataLoader _weatherDataLoader;
        private CancellationTokenSource _pollingCts;

        public WeatherScreenPresenter(IWeatherDataLoader weatherDataLoader)
        {
            _weatherDataLoader = weatherDataLoader;
        }

        public override ScreenId ScreenId => ScreenId.Weather;

        public override void OnShow()
        {
            _pollingCts = new CancellationTokenSource();
            PollWeatherLoopAsync(_pollingCts.Token).Forget();
        }

        public override void OnHide()
        {
            _pollingCts?.Cancel();
            _pollingCts?.Dispose();
            _pollingCts = null;

            _weatherDataLoader.CancelAll();
            View.SetLoading(false);
        }

        public override void Dispose()
        {
            OnHide();
        }

        private async UniTaskVoid PollWeatherLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                View.SetLoading(true);

                try
                {
                    var weatherData = await _weatherDataLoader.LoadWeatherAsync(cancellationToken);

                    View.SetWeather(weatherData.Text, weatherData.IconSprite);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"Weather request failed: {exception.Message}");
                }
                finally
                {
                    View.SetLoading(false);
                }

                try
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(5), cancellationToken: cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }
}
