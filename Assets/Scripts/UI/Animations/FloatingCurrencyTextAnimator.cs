using System.Threading;
using Configs;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UI.Screens.Clicker;
using UnityEngine;
using Utils.Particles;

namespace UI.Animations
{
    internal sealed class FloatingCurrencyTextAnimator
    {
        private readonly ClickerBalanceConfig _config;
        private readonly IParticlePoolService _particlePoolService;

        public FloatingCurrencyTextAnimator(
            ClickerBalanceConfig config,
            IParticlePoolService particlePoolService)
        {
            _config = config;
            _particlePoolService = particlePoolService;
        }

        public async UniTask PlayAsync(ClickerScreenView view, Vector2 startScreenPosition, CancellationToken cancellationToken)
        {
            var textInstance = await _particlePoolService
                .SpawnAsync<TMP_Text>(_config.FloatingCurrencyTextAddressableKey, view.ParticlesRoot, cancellationToken);

            if (textInstance == null || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (textInstance.transform is not RectTransform rect)
            {
                _particlePoolService.ReturnToPool(textInstance);
                return;
            }

            textInstance.DOKill();
            rect.DOKill();

            textInstance.text = view.FloatingCurrencyText;
            textInstance.alpha = 1f;

            var start = view.GetFloatingCurrencyPosition(startScreenPosition);
            rect.anchoredPosition = start;

            var horizontalDirection = Random.value < 0.5f ? -1f : 1f;
            var horizontalMin = Mathf.Min(_config.FloatingCurrencyHorizontalOffsetMin, _config.FloatingCurrencyHorizontalOffsetMax);
            var horizontalMax = Mathf.Max(_config.FloatingCurrencyHorizontalOffsetMin, _config.FloatingCurrencyHorizontalOffsetMax);
            var horizontalOffset = Random.Range(horizontalMin, horizontalMax) * horizontalDirection;

            var downwardMin = Mathf.Min(_config.FloatingCurrencyDownwardOffsetMin, _config.FloatingCurrencyDownwardOffsetMax);
            var downwardMax = Mathf.Max(_config.FloatingCurrencyDownwardOffsetMin, _config.FloatingCurrencyDownwardOffsetMax);
            var downwardOffset = Random.Range(downwardMin, downwardMax);
            var end = start + new Vector2(horizontalOffset, -downwardOffset);

            var arcHeightMin = Mathf.Min(_config.FloatingCurrencyArcHeightMin, _config.FloatingCurrencyArcHeightMax);
            var arcHeightMax = Mathf.Max(_config.FloatingCurrencyArcHeightMin, _config.FloatingCurrencyArcHeightMax);
            var arcHeight = Random.Range(arcHeightMin, arcHeightMax);

            var control = (start + end) * 0.5f + new Vector2(horizontalOffset * _config.FloatingCurrencyHorizontalControlInfluence, arcHeight);
            var duration = Mathf.Max(_config.FloatingCurrencyMinDurationSec, view.FloatingCurrencyDuration);

            var sequence = DOTween.Sequence();
            sequence.Append(DOVirtual.Float(0f, 1f, duration, t =>
            {
                var oneMinusT = 1f - t;
                var curvedPosition =
                    (oneMinusT * oneMinusT * start) +
                    (2f * oneMinusT * t * control) +
                    (t * t * end);
                rect.anchoredPosition = curvedPosition;
            }).SetEase(Ease.OutCubic));
            sequence.Join(textInstance.DOFade(0f, duration));
            sequence.OnComplete(() => _particlePoolService.ReturnToPool(textInstance));
        }
    }
}
