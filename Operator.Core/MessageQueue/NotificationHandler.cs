using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Tgs.MessageQueue;
using Tgs.Notification.Protos;
using TGS.Services.Operator.Notify;

namespace TGS.Services.Operator.MessageQueue;

internal class NotificationHandler : IMessageHandler
{
	private readonly INotifyEventPublisher _notifyEventPublisher;

	public NotificationHandler(INotifyEventPublisher notifyEventPublisher)
	{
		_notifyEventPublisher = notifyEventPublisher ?? throw new ArgumentNullException(nameof(notifyEventPublisher));
	}

	public ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = BroadcastNotificationRequest.Parser.ParseFrom(data.Span);

		var tags = new Dictionary<string, string>
		{
			["Id"] = request.Id.ToGuid().ToString(),
			["UserId"] = request.User,
			["Feature"] = "PushNotify",
			["Type"] = request.Type
		};

		_notifyEventPublisher.PublishEvent(new NotifyEvent(
			tags,
			string.IsNullOrEmpty(request.Payload)
				? JsonDocument.Parse("{}")
				: JsonDocument.Parse(request.Payload)));

		return ValueTask.CompletedTask;
	}
}
