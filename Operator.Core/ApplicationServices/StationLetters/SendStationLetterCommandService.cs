using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;
using Tgs.StationLetterService.Client;

namespace TGS.Services.Operator.ApplicationServices.StationLetters;

internal class SendStationLetterCommandService(IStationLetterClient stationLetterClient) : ICommandService<SendStationLetterCommand>
{
	private readonly IStationLetterClient _stationLetterClient = stationLetterClient ?? throw new ArgumentNullException(nameof(stationLetterClient));

	public ValueTask ExecuteAsync(SendStationLetterCommand command, CancellationToken cancellationToken = default)
	{
		var request = new StationLetterRequest(
			command.PlayerCode,
			command.Receivers,
			command.Subject,
			command.Content,
			command.Operator);

		return _stationLetterClient.SendStationLetterAsync(request, cancellationToken);
	}
}