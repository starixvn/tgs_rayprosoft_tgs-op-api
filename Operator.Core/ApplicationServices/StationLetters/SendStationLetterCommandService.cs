using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;
using Tgs.StationLetterService.Client;

namespace TGS.Services.Operator.ApplicationServices.StationLetters;

internal class SendStationLetterCommandService : ICommandService<SendStationLetterCommand>
{
	private readonly IStationLetterClient _stationLetterClient;

	public SendStationLetterCommandService(IStationLetterClient stationLetterClient)
	{
		_stationLetterClient = stationLetterClient ?? throw new ArgumentNullException(nameof(stationLetterClient));
	}

	public ValueTask ExecuteAsync(SendStationLetterCommand command, CancellationToken cancellationToken = default)
	{
		var request = new StationLetterRequest(
			command.PlayerCode,
			command.Receivers,
			command.Subject,
			command.Content);

		return _stationLetterClient.SendStationLetterAsync(request, cancellationToken);
	}
}
