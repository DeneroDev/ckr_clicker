using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Screens.Weather
{
    public sealed class WeatherScreenView : ScreenViewBase
    {
        [SerializeField] private TMP_Text _weatherText;
        [SerializeField] private Image _weatherIcon;
        [SerializeField] private GameObject _loadingIcon;

        public override ScreenId ScreenId => ScreenId.Weather;

        public void SetLoading(bool isLoading)
        {
            if (_loadingIcon != null)
            {
                _loadingIcon.gameObject.SetActive(isLoading);
            }
        }

        public void SetWeather(string value, Sprite iconSprite)
        {
            if (_weatherText != null)
            {
                _weatherText.text = value;
            }

            if (_weatherIcon != null)
            {
                _weatherIcon.sprite = iconSprite;
                _weatherIcon.enabled = iconSprite != null;
            }
        }
    }
}
