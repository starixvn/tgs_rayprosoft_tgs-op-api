namespace TGS.Services.Operator.Notify;

public interface INotifyEventPublisher
{
	void PublishEvent(NotifyEvent notifyEvent);
}