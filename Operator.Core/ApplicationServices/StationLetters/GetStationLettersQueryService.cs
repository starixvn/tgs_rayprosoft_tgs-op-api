using System;
using System.Collections.Generic;
using System.Threading;
using Tgs.Paging;
using Tgs.ServiceUtility;
using TGS.Services.Operator.StationLetters;

namespace TGS.Services.Operator.ApplicationServices.StationLetters;

internal class GetStationLettersQueryService : IQueryService<GetStationLettersQuery, StationLetter>
{
	private readonly IStationLetterRepository _stationLetterRepository;

	public GetStationLettersQueryService(IStationLetterRepository stationLetterRepository)
	{
		_stationLetterRepository = stationLetterRepository ??
			throw new ArgumentNullException(nameof(stationLetterRepository));
	}

	public IAsyncEnumerable<StationLetter> QueryAsync(
		GetStationLettersQuery query,
		CancellationToken cancellationToken = default)
		=> _stationLetterRepository.QueryStationLettersAsync(query.PlayerCode, query.Paging);
}
