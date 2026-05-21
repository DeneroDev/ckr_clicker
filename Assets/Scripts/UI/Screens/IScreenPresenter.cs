using System;

namespace UI.Screens
{
    public interface IScreenPresenter : IDisposable
    {
        ScreenId ScreenId { get; }
        void Bind(ScreenViewBase view);
        void OnShow();
        void OnHide();
    }
}
