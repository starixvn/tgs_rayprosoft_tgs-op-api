using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Tgs.MessageQueue;
using Tgs.Operator.Client.Protos;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class PublishPlayerLoggedInEventCommandService : ICommandService<PublishPlayerLoggedInEventCommand>
{
	private readonly IMessageSender _messageSender;

	public PublishPlayerLoggedInEventCommandService(
		IMessageSender messageSender)
	{
		_messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
	}

	public ValueTask ExecuteAsync(
		PublishPlayerLoggedInEventCommand command,
		CancellationToken cancellationToken = default)
	{
		var loggedInRequest = new LoggedInEvent
		{
			PlayerCode = command.PlayerCode,
			LoggedInTime = command.LoggedInTime.ToTimestamp()
		};

		return _messageSender.PublishAsync(
			"tgs.operator.player.logged_in",
			loggedInRequest.ToByteArray(),
			cancellationToken);
	}
}
