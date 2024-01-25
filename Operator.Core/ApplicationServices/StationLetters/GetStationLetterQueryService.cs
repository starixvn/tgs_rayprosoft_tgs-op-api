using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;
using Tgs.StationLetterService.Client;

namespace TGS.Services.Operator.ApplicationServices.StationLetters;

internal class GetStationLetterQueryService(IStationLetterClient stationLetterClient) : IGetService<GetStationLetterQuery, StationLetter?>
{
	private readonly IStationLetterClient _stationLetterClient = stationLetterClient ?? throw new ArgumentNullException(nameof(stationLetterClient));

	public ValueTask<StationLetter?> GetAsync(GetStationLetterQuery query, CancellationToken cancellationToken = default)
		=> _stationLetterClient.GetStationLetterAsync(query.LetterId, cancellationToken);
}
