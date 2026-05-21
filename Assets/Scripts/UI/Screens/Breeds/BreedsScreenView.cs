using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Screens.Breeds
{
    public sealed class BreedsScreenView : ScreenViewBase
    {
        [SerializeField] private Transform _listContainer;
        [SerializeField] private BreedListItemView _itemPrefab;
        [SerializeField] private GameObject _breedsLoader;

        private readonly List<BreedListItemView> _spawnedItems = new();
        private readonly Dictionary<string, BreedListItemView> _breedItemsById = new();
        private BreedListItemView _activeDetailsLoaderItem;

        public override ScreenId ScreenId => ScreenId.Breeds;

        public event Action<string, string> BreedSelected;

        public void SetBreeds(IReadOnlyList<BreedShortData> breeds)
        {
            ClearBreedItems();

            for (var i = 0; i < breeds.Count; i++)
            {
                var index = i + 1;
                var breed = breeds[i];
                var view = Instantiate(_itemPrefab, _listContainer);
                var breedId = breed.Id;
                var breedName = breed.Name;
                view.Setup($"{index} - {breedName}", () => OnBreedItemClicked(breedId, breedName));
                _spawnedItems.Add(view);

                if (!string.IsNullOrWhiteSpace(breedId))
                {
                    _breedItemsById[breedId] = view;
                }
            }
        }

        public void SetBreedsLoading(bool isLoading)
        {
            if (_breedsLoader != null)
            {
                _breedsLoader.SetActive(isLoading);
            }
        }

        public void ShowDetailsLoadingFor(string breedId)
        {
            HideDetailsLoading();

            if (string.IsNullOrWhiteSpace(breedId))
            {
                return;
            }

            if (_breedItemsById.TryGetValue(breedId, out var itemView) && itemView != null)
            {
                _activeDetailsLoaderItem = itemView;
                _activeDetailsLoaderItem.SetDetailsLoading(true);
            }
        }

        public void HideDetailsLoading()
        {
            if (_activeDetailsLoaderItem != null)
            {
                _activeDetailsLoaderItem.SetDetailsLoading(false);
            }

            _activeDetailsLoaderItem = null;
        }

        private void ClearBreedItems()
        {
            HideDetailsLoading();

            for (var i = 0; i < _spawnedItems.Count; i++)
            {
                if (_spawnedItems[i] != null)
                {
                    Destroy(_spawnedItems[i].gameObject);
                }
            }

            _spawnedItems.Clear();
            _breedItemsById.Clear();
        }

        private void OnBreedItemClicked(string breedId, string breedName)
        {
            ShowDetailsLoadingFor(breedId);
            BreedSelected?.Invoke(breedId, breedName);
        }
    }

    public readonly struct BreedShortData
    {
        public BreedShortData(string id, string name)
        {
            Id = id;
            Name = name;
        }

        public string Id { get; }
        public string Name { get; }
    }
}
