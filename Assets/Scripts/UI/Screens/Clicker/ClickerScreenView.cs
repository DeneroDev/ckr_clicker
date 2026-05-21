using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UI.Screens.Clicker
{
    public sealed class ClickerScreenView : ScreenViewBase
    {
        [SerializeField] private Button _tapButton;
        [SerializeField] private RectTransform _tapButtonTransform;
        [SerializeField] private TMP_Text _currencyText;
        [SerializeField] private TMP_Text _energyText;

        [SerializeField] private float _tapScale = 0.92f;
        [SerializeField] private float _tapAnimationDuration = 0.1f;

        [Header("Tap VFX")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _tapAudioClip;
        [SerializeField] private RectTransform _particlesRoot;
        [SerializeField] private Vector2 _floatingCurrencyOffset = new(0f, 140f);
        [SerializeField] private float _floatingCurrencyDuration = 0.55f;
        [SerializeField] private string _floatingCurrencyText = "+1";

        public RectTransform ParticlesRoot => _particlesRoot;
        public Vector2 FloatingCurrencyOffset => _floatingCurrencyOffset;
        public float FloatingCurrencyDuration => _floatingCurrencyDuration;
        public string FloatingCurrencyText => _floatingCurrencyText;

        public override ScreenId ScreenId => ScreenId.Clicker;

        public event Action<Vector2> TapRequested;

        private void Start()
        {
            _tapButton.onClick.AddListener(OnTapButtonClicked);
        }

        private void OnDestroy()
        {
            _tapButton.onClick.RemoveListener(OnTapButtonClicked);
        }

        public void SetValues(int currency, int energy)
        {
            _currencyText.text = $"Coins: {currency}";
            _energyText.text = $"Energy: {energy}";
        }

        public void PlayTapFeedback()
        {
            PlayTapSound();

            if (_tapButtonTransform == null)
            {
                return;
            }

            _tapButtonTransform.DOKill();
            _tapButtonTransform.localScale = Vector3.one;
            _tapButtonTransform
                .DOScale(_tapScale, _tapAnimationDuration)
                .SetLoops(2, LoopType.Yoyo);
        }

        public Vector3 GetTapParticleWorldPosition(Vector2 screenPosition)
        {
            if (_particlesRoot == null)
            {
                return _tapButtonTransform != null ? _tapButtonTransform.position : transform.position;
            }

            var canvas = _particlesRoot.GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(_particlesRoot, screenPosition, camera, out var worldPosition))
            {
                return worldPosition;
            }

            return _particlesRoot.position;
        }

        public Vector2 GetFloatingCurrencyPosition(Vector2 screenPosition)
        {
            if (_particlesRoot == null)
            {
                return _tapButtonTransform != null ? _tapButtonTransform.anchoredPosition : Vector2.zero;
            }

            var canvas = _particlesRoot.GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(_particlesRoot, screenPosition, camera, out var localPosition)
                ? localPosition
                : Vector2.zero;
        }

        public Vector2 GetFloatingCurrencyCenterPosition()
        {
            if (_tapButtonTransform == null)
            {
                return Vector2.zero;
            }

            var canvas = _particlesRoot != null
                ? _particlesRoot.GetComponentInParent<Canvas>()
                : null;
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            var screenPoint = RectTransformUtility.WorldToScreenPoint(camera, _tapButtonTransform.position);

            return GetFloatingCurrencyPosition(screenPoint);
        }

        private void PlayTapSound()
        {
            if (_audioSource != null && _tapAudioClip != null)
            {
                _audioSource.PlayOneShot(_tapAudioClip);
            }
        }

        private void OnTapButtonClicked()
        {
            TapRequested?.Invoke(GetCurrentPointerScreenPosition());
        }

        private static Vector2 GetCurrentPointerScreenPosition()
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                return Touchscreen.current.primaryTouch.position.ReadValue();
            }

            if (Mouse.current != null)
            {
                return Mouse.current.position.ReadValue();
            }

            if (Pen.current != null)
            {
                return Pen.current.position.ReadValue();
            }
            
            return Vector2.zero;
        }
    }
}
