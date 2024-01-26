using System;
using System.Collections.Generic;
using System.Threading;
using Tgs.ServiceUtility;
using TGS.Services.Operator.StationLetters;

namespace TGS.Services.Operator.ApplicationServices.StationLetters;

internal class GetStationLettersQueryService(IStationLetterRepository stationLetterRepository) : IQueryService<GetStationLettersQuery, StationLetter>
{
	private readonly IStationLetterRepository _stationLetterRepository = stationLetterRepository ??
			throw new ArgumentNullException(nameof(stationLetterRepository));

	public IAsyncEnumerable<StationLetter> QueryAsync(
		GetStationLettersQuery query,
		CancellationToken cancellationToken = default)
		=> _stationLetterRepository.QueryStationLettersAsync(query.PlayerCode, query.Paging);
}
