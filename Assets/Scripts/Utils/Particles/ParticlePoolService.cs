using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Utils.ResourcesLoader;
using Zenject;

namespace Utils.Particles
{
    public sealed class ParticlePoolService : IParticlePoolService, ITickable
    {
        private readonly IAddressablePrefabLoader _prefabLoader;

        private readonly Dictionary<string, Queue<Component>> _poolByKey = new();
        private readonly Dictionary<string, GameObject> _templateInstanceByKey = new();
        private readonly Dictionary<string, Component> _templateComponentByKey = new();
        private readonly Dictionary<Component, string> _instanceKeyMap = new();
        private readonly List<ParticleSystem> _activeParticleInstances = new();

        public ParticlePoolService(IAddressablePrefabLoader prefabLoader)
        {
            _prefabLoader = prefabLoader;
        }

        public async UniTask PrewarmAsync(string key, int count, Transform parent = null, CancellationToken cancellationToken = default)
        {
            await PrewarmAsync<ParticleSystem>(key, count, parent, cancellationToken);
        }

        public async UniTask PrewarmAsync<T>(string key, int count, Transform parent = null, CancellationToken cancellationToken = default)
            where T : Component
        {
            if (string.IsNullOrWhiteSpace(key) || count <= 0)
            {
                return;
            }

            var templateComponent = await GetOrLoadTemplateComponentAsync<T>(key, cancellationToken);
            if (templateComponent == null)
            {
                return;
            }

            if (!_poolByKey.TryGetValue(key, out var queue))
            {
                queue = new Queue<Component>();
                _poolByKey[key] = queue;
            }

            while (queue.Count < count)
            {
                var instance = UnityEngine.Object.Instantiate(templateComponent, parent);
                instance.gameObject.SetActive(false);
                _instanceKeyMap[instance] = key;
                queue.Enqueue(instance);
            }
        }

        public async UniTask PlayAsync(string key, Vector3 worldPosition, Transform parent = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            var templateParticle = await GetOrLoadTemplateComponentAsync<ParticleSystem>(key, cancellationToken);
            if (templateParticle == null)
            {
                return;
            }

            var instance = GetOrCreateInstance(key, templateParticle, parent);
            instance.transform.SetPositionAndRotation(worldPosition, templateParticle.transform.rotation);
            instance.gameObject.SetActive(true);
            instance.Clear(true);
            instance.Play(true);

            _activeParticleInstances.Add(instance);
        }

        public async UniTask<T> SpawnAsync<T>(string key, Transform parent = null, CancellationToken cancellationToken = default)
            where T : Component
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            var template = await GetOrLoadTemplateComponentAsync<T>(key, cancellationToken);
            if (template == null)
            {
                return null;
            }

            var instance = GetOrCreateInstance(key, template, parent);
            instance.gameObject.SetActive(true);
            return instance;
        }

        public void Tick()
        {
            for (var i = _activeParticleInstances.Count - 1; i >= 0; i--)
            {
                var instance = _activeParticleInstances[i];
                if (instance == null)
                {
                    _activeParticleInstances.RemoveAt(i);
                    continue;
                }

                if (instance.IsAlive(true))
                {
                    continue;
                }

                ReturnToPool(instance);
            }
        }

        public void ReturnToPool(Component component)
        {
            if (component == null)
            {
                return;
            }

            if (component is ParticleSystem particle)
            {
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                _activeParticleInstances.Remove(particle);
            }

            component.gameObject.SetActive(false);

            if (!_instanceKeyMap.TryGetValue(component, out var key))
            {
                UnityEngine.Object.Destroy(component.gameObject);
                return;
            }

            if (!_poolByKey.TryGetValue(key, out var queue))
            {
                queue = new Queue<Component>();
                _poolByKey[key] = queue;
            }

            queue.Enqueue(component);
        }

        public void Dispose()
        {
            var destroyedObjects = new HashSet<GameObject>();

            for (var i = 0; i < _activeParticleInstances.Count; i++)
            {
                var instance = _activeParticleInstances[i];
                if (instance != null)
                {
                    destroyedObjects.Add(instance.gameObject);
                    UnityEngine.Object.Destroy(instance.gameObject);
                }
            }

            _activeParticleInstances.Clear();

            foreach (var queue in _poolByKey.Values)
            {
                while (queue.Count > 0)
                {
                    var instance = queue.Dequeue();
                    if (instance != null)
                    {
                        if (destroyedObjects.Add(instance.gameObject))
                        {
                            UnityEngine.Object.Destroy(instance.gameObject);
                        }
                    }
                }
            }

            foreach (var component in _instanceKeyMap.Keys)
            {
                if (component != null && destroyedObjects.Add(component.gameObject))
                {
                    UnityEngine.Object.Destroy(component.gameObject);
                }
            }

            _poolByKey.Clear();

            foreach (var template in _templateInstanceByKey.Values)
            {
                if (template != null)
                {
                    _prefabLoader.ReleaseInstance(template);
                }
            }

            _templateInstanceByKey.Clear();
            _templateComponentByKey.Clear();
            _instanceKeyMap.Clear();
        }

        private async UniTask<T> GetOrLoadTemplateComponentAsync<T>(string key, CancellationToken cancellationToken)
            where T : Component
        {
            if (_templateComponentByKey.TryGetValue(key, out var cachedTemplateComponent) && cachedTemplateComponent != null)
            {
                if (cachedTemplateComponent is T typedTemplate)
                {
                    return typedTemplate;
                }

                Debug.LogError($"Addressable key '{key}' was registered with '{cachedTemplateComponent.GetType().Name}' and cannot be used as '{typeof(T).Name}'.");
                return null;
            }

            var templateInstance = await _prefabLoader.InstantiateAsync(key, null, cancellationToken);
            templateInstance.SetActive(false);

            var templateComponent = templateInstance.GetComponent<T>();
            if (templateComponent == null)
            {
                templateComponent = templateInstance.GetComponentInChildren<T>(true);
            }

            if (templateComponent == null)
            {
                Debug.LogError($"Addressable prefab with key '{key}' does not contain a component of type '{typeof(T).Name}'.");
                _prefabLoader.ReleaseInstance(templateInstance);
                return null;
            }

            _templateInstanceByKey[key] = templateInstance;
            _templateComponentByKey[key] = templateComponent;
            return templateComponent;
        }

        private T GetOrCreateInstance<T>(string key, T templateComponent, Transform parent)
            where T : Component
        {
            if (_poolByKey.TryGetValue(key, out var queue) && queue.Count > 0)
            {
                var pooled = queue.Dequeue();
                if (pooled == null)
                {
                    return GetOrCreateInstance(key, templateComponent, parent);
                }

                if (pooled is not T typedPooled)
                {
                    UnityEngine.Object.Destroy(pooled.gameObject);
                    return GetOrCreateInstance(key, templateComponent, parent);
                }

                if (parent != null)
                {
                    typedPooled.transform.SetParent(parent, false);
                }

                return typedPooled;
            }

            var created = UnityEngine.Object.Instantiate(templateComponent, parent);
            _instanceKeyMap[created] = key;
            return created;
        }
    }
}
