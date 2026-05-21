using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Utils.ResourcesLoader
{
    public sealed class AddressablePrefabLoader : IAddressablePrefabLoader
    {
        public async UniTask<GameObject> InstantiateAsync(string key, Transform parent, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Addressable key cannot be null or empty", nameof(key));
            }

            var handle = Addressables.InstantiateAsync(key, parent);
            try
            {
                return await handle.ToUniTask(cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                if (handle.IsValid() && handle.IsDone && handle.Result != null)
                {
                    Addressables.ReleaseInstance(handle.Result);
                }

                throw;
            }
        }

        public void ReleaseInstance(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            Addressables.ReleaseInstance(instance);
        }
    }
}
