using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace UI.Popups
{
    public interface IPopupService : IDisposable
    {
        UniTask ShowAsync<TPayload>(PopupId popupId, TPayload payload, CancellationToken cancellationToken = default);
        void Hide(PopupId popupId);
        void HideAll();
    }
}
