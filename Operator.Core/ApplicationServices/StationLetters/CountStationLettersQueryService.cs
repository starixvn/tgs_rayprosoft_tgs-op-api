using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;
using TGS.Services.Operator.StationLetters;

namespace TGS.Services.Operator.ApplicationServices.StationLetters;

internal class CountStationLettersQueryService(IStationLetterRepository stationLetterRepository) : IGetService<GetStationLettersQuery, int>
{
	private readonly IStationLetterRepository _stationLetterRepository = stationLetterRepository ?? throw new ArgumentNullException(nameof(stationLetterRepository));

	public async ValueTask<int> GetAsync(GetStationLettersQuery query, CancellationToken cancellationToken = default) =>
		await _stationLetterRepository
			.QueryStationLettersTotalCountAsync(query.PlayerCode)
			.ConfigureAwait(false);
}
