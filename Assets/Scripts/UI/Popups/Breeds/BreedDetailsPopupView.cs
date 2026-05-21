using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Popups.Breeds
{
    public sealed class BreedDetailsPopupView : MonoBehaviour, IPopupView, IPopupPayloadReceiver<BreedDetailsPopupData>
    {
        [SerializeField] private RectTransform _windowRect;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _descriptionText;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _okButton;
        [SerializeField, Min(0f)] private float _descriptionMinHeight = 120f;
        [SerializeField, Min(0f)] private float _descriptionMaxHeight = 420f;
        [SerializeField, Min(0f)] private float _descriptionVerticalPadding = 8f;
        [SerializeField] private bool _allowShrinkWindow = true;

        private void Awake()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Hide);
            }

            if (_okButton != null)
            {
                _okButton.onClick.AddListener(Hide);
            }

            Hide();
        }

        private void OnDestroy()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(Hide);
            }

            if (_okButton != null)
            {
                _okButton.onClick.RemoveListener(Hide);
            }
        }

        public void SetPayload(BreedDetailsPopupData payload)
        {
            if (_titleText != null)
            {
                _titleText.text = payload.Title;
            }

            if (_descriptionText != null)
            {
                _descriptionText.text = payload.Description;
                UpdateWindowHeight(payload.Description);
            }
        }

        private void UpdateWindowHeight(string description)
        {
            if (_descriptionText == null)
            {
                return;
            }

            var descriptionRect = _descriptionText.rectTransform;
            var width = descriptionRect.rect.width;

            if (width <= 0f)
            {
                return;
            }

            var preferredSize = _descriptionText.GetPreferredValues(description ?? string.Empty, width, Mathf.Infinity);
            var minHeight = Mathf.Max(0f, _descriptionMinHeight);
            var maxHeight = Mathf.Max(minHeight, _descriptionMaxHeight);
            var targetDescriptionHeight = Mathf.Clamp(preferredSize.y + _descriptionVerticalPadding, minHeight, maxHeight);

            if (_windowRect == null)
            {
                return;
            }

            if (!_allowShrinkWindow)
            {
                targetDescriptionHeight = Mathf.Max(_windowRect.rect.height, targetDescriptionHeight);
            }

            _windowRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetDescriptionHeight);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_windowRect);

            if (_windowRect.parent is RectTransform parentRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
            }
        }

        public void Show()
        {
            if (_windowRect != null)
            {
                _windowRect.gameObject.SetActive(true);
                return;
            }

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (_windowRect != null)
            {
                _windowRect.gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(false);
        }
    }
}
