using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;
using Tgs.StationLetterService.Client;

namespace TGS.Services.Operator.ApplicationServices.StationLetters;

internal class SetHaveReadV2CommandService(IStationLetterClient stationLetterClient) : ICommandService<SetHaveReadV2Command>
{
	private readonly IStationLetterClient _stationLetterClient = stationLetterClient ?? throw new ArgumentNullException(nameof(stationLetterClient));

	public async ValueTask ExecuteAsync(SetHaveReadV2Command command, CancellationToken cancellationToken = default)
	{
		var letter = await _stationLetterClient.GetStationLetterAsync(
			command.LetterId,
			cancellationToken).ConfigureAwait(false);

		if (letter is null || letter.Receiver != command.PlayerCode)
			return;

		await _stationLetterClient.MarkStationLetterHaveReadAsync(
			command.LetterId,
			cancellationToken).ConfigureAwait(false);
	}
}