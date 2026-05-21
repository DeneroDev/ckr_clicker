using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Utils.Particles
{
    public interface IParticlePoolService : IDisposable
    {
        UniTask PrewarmAsync(string key, int count, Transform parent = null, CancellationToken cancellationToken = default);
        UniTask PrewarmAsync<T>(string key, int count, Transform parent = null, CancellationToken cancellationToken = default)
            where T : Component;
        UniTask PlayAsync(string key, Vector3 worldPosition, Transform parent = null, CancellationToken cancellationToken = default);
        UniTask<T> SpawnAsync<T>(string key, Transform parent = null, CancellationToken cancellationToken = default)
            where T : Component;
        void ReturnToPool(Component component);
    }
}
