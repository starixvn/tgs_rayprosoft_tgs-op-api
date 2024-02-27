using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;
using Tgs.StationLetterService.Client;

namespace TGS.Services.Operator.ApplicationServices.StationLetters;

internal class ReplyLetterCommandService(IStationLetterClient stationLetterClient) : ICommandService<ReplyLetterCommand>
{
	private readonly IStationLetterClient _stationLetterClient = stationLetterClient ?? throw new ArgumentNullException(nameof(stationLetterClient));

	public async ValueTask ExecuteAsync(ReplyLetterCommand command, CancellationToken cancellationToken = default)
	{
		var letter = await _stationLetterClient.GetStationLetterAsync(
			command.LetterId,
			cancellationToken).ConfigureAwait(false);

		if (letter is null || letter.Receiver != command.PlayerCode)
			return;

		var request = new ReplyStationLetterRequest(
			command.PlayerCode,
			letter.Sender,
			command.Content,
			command.Operator);

		await _stationLetterClient.ReplyStationLetterAsync(
			command.LetterId,
			request,
			cancellationToken).ConfigureAwait(false);
	}
}