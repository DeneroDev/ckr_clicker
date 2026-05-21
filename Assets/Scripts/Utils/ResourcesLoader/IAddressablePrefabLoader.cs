using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Utils.ResourcesLoader
{
    public interface IAddressablePrefabLoader
    {
        UniTask<GameObject> InstantiateAsync(string key, Transform parent, CancellationToken cancellationToken = default);
        void ReleaseInstance(GameObject instance);
    }
}
