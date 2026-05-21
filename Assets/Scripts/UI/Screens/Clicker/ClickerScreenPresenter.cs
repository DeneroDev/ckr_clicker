using System;
using System.Threading;
using Configs;
using Core;
using Core.Clicker;
using Cysharp.Threading.Tasks;
using TMPro;
using UI.Animations;
using UnityEngine;
using Utils.Particles;

namespace UI.Screens.Clicker
{
    public sealed class ClickerScreenPresenter : ScreenPresenterBase<ClickerScreenView>
    {
        private readonly ClickerBalanceConfig _config;
        private readonly IParticlePoolService _particlePoolService;
        private readonly ClickerCore _clickerCore;
        private readonly FloatingCurrencyTextAnimator _floatingCurrencyAnimator;
        private CancellationTokenSource _runtimeCts;

        public ClickerScreenPresenter(
            ClickerBalanceConfig config,
            IParticlePoolService particlePoolService)
        {
            _config = config;
            _particlePoolService = particlePoolService;
            _clickerCore = new ClickerCore(_config);
            _floatingCurrencyAnimator = new FloatingCurrencyTextAnimator(_config, _particlePoolService);
        }

        public override ScreenId ScreenId => ScreenId.Clicker;

        protected override void OnBound()
        {
            _clickerCore.Initialize();
            View.TapRequested += OnTapRequested;
            SyncView();
        }

        public override void OnShow()
        {
            _runtimeCts = new CancellationTokenSource();
            AutoCollectLoopAsync(_runtimeCts.Token).Forget();
            EnergyRegenLoopAsync(_runtimeCts.Token).Forget();

            if (!string.IsNullOrWhiteSpace(_config.TapParticleAddressableKey) && _config.TapParticlePrewarmCount > 0)
            {
                _particlePoolService
                    .PrewarmAsync(_config.TapParticleAddressableKey, _config.TapParticlePrewarmCount, View.ParticlesRoot, cancellationToken: _runtimeCts.Token)
                    .Forget();
            }

            if (!string.IsNullOrWhiteSpace(_config.FloatingCurrencyTextAddressableKey) && _config.FloatingCurrencyTextPrewarmCount > 0)
            {
                _particlePoolService
                    .PrewarmAsync<TMP_Text>(_config.FloatingCurrencyTextAddressableKey, _config.FloatingCurrencyTextPrewarmCount, View.ParticlesRoot, _runtimeCts.Token)
                    .Forget();
            }
        }

        public override void OnHide()
        {
            _runtimeCts?.Cancel();
            _runtimeCts?.Dispose();
            _runtimeCts = null;
        }

        public override void Dispose()
        {
            OnHide();
            if (View != null)
            {
                View.TapRequested -= OnTapRequested;
            }
        }

        private void OnTapRequested(Vector2 screenPosition)
        {
            if (!_clickerCore.TryTapCollect())
            {
                return;
            }

            PlayTapEffects(screenPosition);
            SyncView();
        }

        private void PlayTapEffects(Vector2 tapScreenPosition)
        {
            if (_runtimeCts != null)
            {
                var tapParticlePosition = View.GetTapParticleWorldPosition(tapScreenPosition);
                _particlePoolService
                    .PlayAsync(_config.TapParticleAddressableKey, tapParticlePosition, View.ParticlesRoot, cancellationToken: _runtimeCts.Token)
                    .Forget();
            }

            if ( _runtimeCts != null)
            {
                _floatingCurrencyAnimator.PlayAsync(View, tapScreenPosition, _runtimeCts.Token).Forget();
            }

            View.PlayTapFeedback();
        }
        
        private void PlayTapEffects()
        {
            var screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            PlayTapEffects(screenCenter);
        }

        private async UniTaskVoid AutoCollectLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_config.AutoCollectIntervalSec), cancellationToken: cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (_clickerCore.TryAutoCollect())
                {
                    PlayTapEffects();
                    SyncView();
                }
            }
        }

        private async UniTaskVoid EnergyRegenLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_config.EnergyRegenIntervalSec), cancellationToken: cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                _clickerCore.RegenerateEnergy();
                SyncView();
            }
        }

        private void SyncView()
        {
            View.SetValues(_clickerCore.Currency, _clickerCore.Energy);
        }
    }
}
