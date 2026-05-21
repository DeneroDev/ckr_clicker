using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UI.Screens.Breeds;
using UnityEngine;
using UnityEngine.Networking;
using Utils.Requests;

namespace Core.Breeds
{
    public interface IBreedsDataLoader
    {
        UniTask<IReadOnlyList<BreedShortData>> LoadBreedsAsync(CancellationToken cancellationToken);
        UniTask<string> LoadBreedDetailsAsync(string breedId, CancellationToken cancellationToken);
        void CancelBreedDetailsLoading();
        void CancelAll();
    }

    public sealed class BreedsDataLoader : IBreedsDataLoader
    {
        private const string ListOwnerKey = "screen.breeds.list";
        private const string DetailOwnerKey = "screen.breeds.detail";
        private const string BreedsListUrl = "https://dogapi.dog/api/v2/breeds";
        private const string BreedDetailsUrlTemplate = "https://dogapi.dog/api/v2/breeds/{0}";

        private readonly IRequestQueue _requestQueue;

        public BreedsDataLoader(IRequestQueue requestQueue)
        {
            _requestQueue = requestQueue;
        }

        public UniTask<IReadOnlyList<BreedShortData>> LoadBreedsAsync(CancellationToken cancellationToken)
        {
            return _requestQueue.Enqueue(
                ListOwnerKey,
                token => FetchBreedsAsync(token),
                cancellationToken);
        }

        public UniTask<string> LoadBreedDetailsAsync(string breedId, CancellationToken cancellationToken)
        {
            return _requestQueue.Enqueue(
                DetailOwnerKey,
                token => FetchBreedDetailsAsync(breedId, token),
                cancellationToken);
        }

        public void CancelBreedDetailsLoading()
        {
            _requestQueue.CancelByOwner(DetailOwnerKey);
        }

        public void CancelAll()
        {
            _requestQueue.CancelByOwner(ListOwnerKey);
            _requestQueue.CancelByOwner(DetailOwnerKey);
        }

        private static async UniTask<IReadOnlyList<BreedShortData>> FetchBreedsAsync(CancellationToken cancellationToken)
        {
            using var request = UnityWebRequest.Get(BreedsListUrl);
            await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new InvalidOperationException($"Dog API (breeds) error: {request.error}");
            }

            var response = JsonUtility.FromJson<BreedsResponse>(request.downloadHandler.text);
            var result = new List<BreedShortData>(10);

            if (response?.data == null)
            {
                return result;
            }

            for (var i = 0; i < response.data.Length && result.Count < 10; i++)
            {
                var item = response.data[i];
                var id = item?.id;
                var name = item?.attributes?.name;
                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                result.Add(new BreedShortData(id, name));
            }

            return result;
        }

        private static async UniTask<string> FetchBreedDetailsAsync(string breedId, CancellationToken cancellationToken)
        {
            using var request = UnityWebRequest.Get(string.Format(BreedDetailsUrlTemplate, breedId));
            await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new InvalidOperationException($"Dog API (details) error: {request.error}");
            }

            var response = JsonUtility.FromJson<BreedDetailsResponse>(request.downloadHandler.text);
            var description = response?.data?.attributes?.description;
            return string.IsNullOrWhiteSpace(description) ? "No description available." : description;
        }

        [Serializable]
        private sealed class BreedsResponse
        {
            public BreedData[] data;
        }

        [Serializable]
        private sealed class BreedDetailsResponse
        {
            public BreedData data;
        }

        [Serializable]
        private sealed class BreedData
        {
            public string id;
            public BreedAttributes attributes;
        }

        [Serializable]
        private sealed class BreedAttributes
        {
            public string name;
            public string description;
        }
    }
}
