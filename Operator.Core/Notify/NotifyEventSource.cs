using System;
using System.Reactive.Subjects;

namespace TGS.Services.Operator.Notify;

internal class NotifyEventSource : IObservable<NotifyEvent>, INotifyEventPublisher, IDisposable
{
	private readonly Subject<NotifyEvent> _subject = new();
	private bool _disposedValue;

	public void Dispose()
	{
		// 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	public void PublishEvent(NotifyEvent notifyEvent)
		=> _subject.OnNext(notifyEvent);

	public IDisposable Subscribe(IObserver<NotifyEvent> observer)
		=> _subject.Subscribe(observer);

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				_subject.OnCompleted();
				_subject.Dispose();
			}

			_disposedValue = true;
		}
	}
}