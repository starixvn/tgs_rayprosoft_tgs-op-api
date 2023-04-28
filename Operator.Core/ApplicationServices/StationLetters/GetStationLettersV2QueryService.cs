using System;
using System.Collections.Generic;
using System.Threading;
using Tgs.ServiceUtility;
using Tgs.StationLetterService.Client;

namespace TGS.Services.Operator.ApplicationServices.StationLetters;

internal class GetStationLettersV2QueryService : IQueryService<GetStationLettersV2Query, StationLetter>
{
	private readonly IStationLetterClient _stationLetterClient;

	public GetStationLettersV2QueryService(IStationLetterClient stationLetterClient)
	{
		_stationLetterClient = stationLetterClient ?? throw new ArgumentNullException(nameof(stationLetterClient));
	}

	public IAsyncEnumerable<StationLetter> QueryAsync(GetStationLettersV2Query query, CancellationToken cancellationToken = default)
		=> _stationLetterClient.ListStationLettersAsync(query.PlayerCode, cancellationToken);
}
