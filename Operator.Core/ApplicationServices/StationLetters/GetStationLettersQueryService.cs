using System;
using System.Collections.Generic;
using System.Threading;
using Tgs.ServiceUtility;
using Tgs.Paging;
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

    public IAsyncEnumerable<StationLetter> QueryAsync(GetStationLettersQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Paging.ShowAll)
            return _stationLetterRepository.QueryStationLettersAsync(query.PlayerCode);

        var paging = new Paging<StationLetter>
        {
            Page = query.Paging.Page,
            RowCount = query.Paging.Rows
        };

        paging.SortByPropertyName(query.Paging.SortBy, query.Paging.IsDesc);

        return _stationLetterRepository.QueryStationLettersAsync(query.PlayerCode, paging);
    }
}
