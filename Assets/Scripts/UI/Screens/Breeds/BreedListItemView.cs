using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Screens.Breeds
{
    public sealed class BreedListItemView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private GameObject _detailsLoader;

        private Action _onClick;

        public void Setup(string text, Action onClick)
        {
            _label.text = text;
            _onClick = onClick;
            SetDetailsLoading(false);

            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(HandleClick);
        }

        public void SetDetailsLoading(bool isLoading)
        {
            if (_detailsLoader != null)
            {
                _detailsLoader.SetActive(isLoading);
            }
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveAllListeners();
        }

        private void HandleClick()
        {
            _onClick?.Invoke();
        }
    }
}
