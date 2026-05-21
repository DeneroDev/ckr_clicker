using UnityEngine;

namespace Utils
{
    public sealed class LoaderSpinner : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _degreesPerSecond = -180f;

        private void Awake()
        {
            if (_target == null)
            {
                _target = transform;
            }
        }

        private void Update()
        {
            if (_target == null)
            {
                return;
            }

            _target.Rotate(0f, 0f, _degreesPerSecond * Time.unscaledDeltaTime, Space.Self);
        }
    }
}
