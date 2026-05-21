namespace UI.Popups
{
    public interface IPopupPayloadReceiver<in TPayload>
    {
        void SetPayload(TPayload payload);
    }
}
