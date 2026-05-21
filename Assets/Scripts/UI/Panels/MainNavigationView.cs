using System;
using DG.Tweening;
using UI.Screens;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Panels
{
    public sealed class MainNavigationView : MonoBehaviour
    {
        [SerializeField] private Button _clickerButton;
        [SerializeField] private Button _weatherButton;
        [SerializeField] private Button _breedsButton;

        [SerializeField] private float _selectedScale = 1f;
        [SerializeField] private float _normalScale = 1f;
        [SerializeField] private float _animationDuration = 0.2f;

        public event Action<ScreenId> TabSelected;

        private void Awake()
        {
            _clickerButton.onClick.AddListener(() => TabSelected?.Invoke(ScreenId.Clicker));
            _weatherButton.onClick.AddListener(() => TabSelected?.Invoke(ScreenId.Weather));
            _breedsButton.onClick.AddListener(() => TabSelected?.Invoke(ScreenId.Breeds));
        }

        private void OnDestroy()
        {
            _clickerButton.onClick.RemoveAllListeners();
            _weatherButton.onClick.RemoveAllListeners();
            _breedsButton.onClick.RemoveAllListeners();
        }

        public void SetSelected(ScreenId screenId)
        {
            AnimateButton(_clickerButton, screenId == ScreenId.Clicker);
            AnimateButton(_weatherButton, screenId == ScreenId.Weather);
            AnimateButton(_breedsButton, screenId == ScreenId.Breeds);
        }

        private void AnimateButton(Button button, bool isSelected)
        {
            if (button == null)
            {
                return;
            }

            var targetScale = isSelected ? _selectedScale : _normalScale;
            button.interactable = !isSelected;
            button.transform.DOKill();
            button.transform.DOScale(targetScale, _animationDuration);
        }
    }
}
