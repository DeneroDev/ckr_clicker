using System;
using System.Threading;
using Core.Breeds;
using Cysharp.Threading.Tasks;
using UI.Popups;
using UI.Popups.Breeds;
using UnityEngine;

namespace UI.Screens.Breeds
{
    public sealed class BreedsScreenPresenter : ScreenPresenterBase<BreedsScreenView>
    {
        private readonly IBreedsDataLoader _breedsDataLoader;
        private readonly IPopupService _popupService;
        private CancellationTokenSource _showCts;
        private int _detailsRequestVersion;

        public BreedsScreenPresenter(IBreedsDataLoader breedsDataLoader, IPopupService popupService)
        {
            _breedsDataLoader = breedsDataLoader;
            _popupService = popupService;
        }

        public override ScreenId ScreenId => ScreenId.Breeds;

        protected override void OnBound()
        {
            View.BreedSelected += OnBreedSelected;
        }

        public override void OnShow()
        {
            _showCts?.Cancel();
            _showCts?.Dispose();
            _showCts = new CancellationTokenSource();
            LoadBreedsAsync(_showCts.Token).Forget();
        }

        public override void OnHide()
        {
            _showCts?.Cancel();
            _showCts?.Dispose();
            _showCts = null;

            _breedsDataLoader.CancelAll();
            _detailsRequestVersion++;

            View.SetBreedsLoading(false);
            View.HideDetailsLoading();
            _popupService.Hide(PopupId.BreedDetails);
        }

        public override void Dispose()
        {
            OnHide();
            if (View != null)
            {
                View.BreedSelected -= OnBreedSelected;
            }
        }

        private async UniTaskVoid LoadBreedsAsync(CancellationToken cancellationToken)
        {
            View.SetBreedsLoading(true);

            try
            {
                var breeds = await _breedsDataLoader.LoadBreedsAsync(cancellationToken);

                View.SetBreeds(breeds);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to load breeds: {exception.Message}");
            }
            finally
            {
                View.SetBreedsLoading(false);
            }
        }

        private void OnBreedSelected(string breedId, string breedName)
        {
            if (string.IsNullOrWhiteSpace(breedId))
            {
                return;
            }

            if (_showCts == null)
            {
                return;
            }

            _breedsDataLoader.CancelBreedDetailsLoading();
            var requestVersion = ++_detailsRequestVersion;
            LoadBreedDetailsAsync(breedId, breedName, requestVersion, _showCts.Token).Forget();
        }

        private async UniTaskVoid LoadBreedDetailsAsync(string breedId, string breedName, int requestVersion, CancellationToken cancellationToken)
        {
            _popupService.Hide(PopupId.BreedDetails);

            try
            {
                var description = await _breedsDataLoader.LoadBreedDetailsAsync(breedId, cancellationToken);

                if (requestVersion != _detailsRequestVersion || cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var payload = new BreedDetailsPopupData(breedName, description);
                await _popupService.ShowAsync(PopupId.BreedDetails, payload, cancellationToken);

                if (requestVersion != _detailsRequestVersion || cancellationToken.IsCancellationRequested)
                {
                    _popupService.Hide(PopupId.BreedDetails);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to load breed details: {exception.Message}");
            }
            finally
            {
                if (requestVersion == _detailsRequestVersion)
                {
                    View.HideDetailsLoading();
                }
            }
        }
    }
}
