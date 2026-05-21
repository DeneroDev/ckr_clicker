using DG.Tweening;
using UnityEngine;

namespace UI.Screens
{
    public abstract class ScreenViewBase : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _fadeDuration = 0.2f;

        public abstract ScreenId ScreenId { get; }

        protected virtual void Awake()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        public virtual void Show(bool instant = false)
        {
            gameObject.SetActive(true);

            if (_canvasGroup == null || instant)
            {
                return;
            }

            _canvasGroup.DOKill();
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.DOFade(1f, _fadeDuration);
        }

        public virtual void Hide(bool instant = false)
        {
            if (_canvasGroup == null || instant)
            {
                gameObject.SetActive(false);
                return;
            }

            _canvasGroup.DOKill();
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.DOFade(0f, _fadeDuration).OnComplete(() => gameObject.SetActive(false));
        }
    }
}
