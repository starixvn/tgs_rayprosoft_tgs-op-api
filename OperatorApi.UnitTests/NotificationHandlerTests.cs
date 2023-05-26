using Google.Protobuf;
using NSubstitute;
using Tgs.Notification.Protos;
using TGS.Services.Operator.MessageQueue;
using TGS.Services.Operator.Notify;

namespace TGS.Tests.OperatorApi.UnitTests;

public class NotificationHandlerTests
{
	[Test]
	public async Task 收到推播通知()
	{
		var fakeNotifyEventPublisher = Substitute.For<INotifyEventPublisher>();

		var sut = new NotificationHandler(fakeNotifyEventPublisher);

		var request = new BroadcastNotificationRequest
		{
			Id = Guid.NewGuid(),
			Type = "Test",
			User = "test1",
			Payload = "{}"
		};

		await sut.HandleAsync(request.ToByteArray());

		fakeNotifyEventPublisher.Received(1)
			.PublishEvent(Arg.Any<NotifyEvent>());
	}
}
