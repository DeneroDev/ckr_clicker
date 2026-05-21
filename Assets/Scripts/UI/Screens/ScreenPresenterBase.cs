using System;

namespace UI.Screens
{
    public abstract class ScreenPresenterBase<TView> : IScreenPresenter where TView : ScreenViewBase
    {
        protected TView View { get; private set; }

        public abstract ScreenId ScreenId { get; }

        public void Bind(ScreenViewBase view)
        {
            if (view is not TView typedView)
            {
                throw new InvalidCastException($"View type mismatch for {GetType().Name}. Expected {typeof(TView).Name}, got {view.GetType().Name}.");
            }

            View = typedView;
            OnBound();
        }

        public virtual void OnShow()
        {
        }

        public virtual void OnHide()
        {
        }

        public virtual void Dispose()
        {
        }

        protected virtual void OnBound()
        {
        }
    }
}
