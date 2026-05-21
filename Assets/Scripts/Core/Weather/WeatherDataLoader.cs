using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Utils.Requests;

namespace Core.Weather
{
    public interface IWeatherDataLoader
    {
        UniTask<WeatherDisplayData> LoadWeatherAsync(CancellationToken cancellationToken);
        void CancelAll();
    }

    public sealed class WeatherDataLoader : IWeatherDataLoader, IDisposable
    {
        private const string OwnerKey = "screen.weather";
        private const string ForecastUrl = "https://api.weather.gov/gridpoints/TOP/32,81/forecast";

        private readonly IRequestQueue _requestQueue;
        private readonly Dictionary<string, Sprite> _iconCache = new();

        public WeatherDataLoader(IRequestQueue requestQueue)
        {
            _requestQueue = requestQueue;
        }

        public UniTask<WeatherDisplayData> LoadWeatherAsync(CancellationToken cancellationToken)
        {
            return _requestQueue.Enqueue(
                OwnerKey,
                token => FetchWeatherDataAsync(token),
                cancellationToken);
        }

        public void CancelAll()
        {
            _requestQueue.CancelByOwner(OwnerKey);
        }

        public void Dispose()
        {
            CancelAll();

            foreach (var pair in _iconCache)
            {
                if (pair.Value != null)
                {
                    UnityEngine.Object.Destroy(pair.Value);
                }
            }

            _iconCache.Clear();
        }

        private async UniTask<WeatherDisplayData> FetchWeatherDataAsync(CancellationToken cancellationToken)
        {
            using var request = UnityWebRequest.Get(ForecastUrl);
            await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new InvalidOperationException($"Weather API error: {request.error}");
            }

            var response = JsonUtility.FromJson<WeatherResponse>(request.downloadHandler.text);
            var period = response?.properties?.periods != null && response.properties.periods.Length > 0
                ? response.properties.periods[0]
                : null;

            if (period == null)
            {
                return new WeatherDisplayData("Сегодня - N/A", null);
            }

            var iconSprite = await ResolveIconSpriteAsync(period.icon, cancellationToken);
            return new WeatherDisplayData($"Сегодня - {period.temperature}F", iconSprite);
        }

        private async UniTask<Sprite> ResolveIconSpriteAsync(string iconUrl, CancellationToken cancellationToken)
        {
            var normalizedUrl = NormalizeIconUrl(iconUrl);
            if (string.IsNullOrWhiteSpace(normalizedUrl))
            {
                return null;
            }

            if (_iconCache.TryGetValue(normalizedUrl, out var cached) && cached != null)
            {
                return cached;
            }

            using var request = UnityWebRequestTexture.GetTexture(normalizedUrl);
            await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"Weather icon request failed: {request.error}");
                return null;
            }

            var texture = DownloadHandlerTexture.GetContent(request);
            if (texture == null)
            {
                return null;
            }

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);

            _iconCache[normalizedUrl] = sprite;
            return sprite;
        }

        private static string NormalizeIconUrl(string rawUrl)
        {
            if (string.IsNullOrWhiteSpace(rawUrl))
            {
                return null;
            }

            var separatorIndex = rawUrl.IndexOf(',');
            if (separatorIndex >= 0)
            {
                return rawUrl.Substring(0, separatorIndex);
            }

            return rawUrl;
        }

        [Serializable]
        private sealed class WeatherResponse
        {
            public WeatherProperties properties;
        }

        [Serializable]
        private sealed class WeatherProperties
        {
            public WeatherPeriod[] periods;
        }

        [Serializable]
        private sealed class WeatherPeriod
        {
            public int temperature;
            public string icon;
        }
    }

    public readonly struct WeatherDisplayData
    {
        public WeatherDisplayData(string text, Sprite iconSprite)
        {
            Text = text;
            IconSprite = iconSprite;
        }

        public string Text { get; }
        public Sprite IconSprite { get; }
    }
}
