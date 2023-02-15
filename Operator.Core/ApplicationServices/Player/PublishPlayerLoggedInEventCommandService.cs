using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Tgs.MessageQueue;
using Tgs.Operator.Client.Protos;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class PublishPlayerLoggedInEventCommandService : ICommandService<PublishPlayerLoggedInEventCommand>
{
	private readonly IMessageSender _messageSender;
	private readonly ILogger<PublishPlayerLoggedInEventCommandService> _logger;

	public PublishPlayerLoggedInEventCommandService(
		IMessageSender messageSender,
		ILogger<PublishPlayerLoggedInEventCommandService> logger)
	{
		_messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async ValueTask ExecuteAsync(
		PublishPlayerLoggedInEventCommand command,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var loggedInRequest = new LoggedInEvent
			{
				PlayerCode = command.PlayerCode, 
				LoggedInTime = command.LoggedInTime.ToTimestamp()
			};

			await _messageSender.PublishAsync(
				"tgs.operator.player.logged_in",
				loggedInRequest.ToByteArray(),
				cancellationToken)
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "fail to send logged-in event of player '{playerCode}'", command.PlayerCode);
		}
	}
}
