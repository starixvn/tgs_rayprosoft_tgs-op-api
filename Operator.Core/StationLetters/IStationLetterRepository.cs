using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tgs.Paging;

namespace TGS.Services.Operator.StationLetters;

public interface IStationLetterRepository
{
	IAsyncEnumerable<StationLetter> QueryStationLettersAsync(string playerCode, Paging<StationLetter> paging);

	IAsyncEnumerable<StationLetter> QueryStationLettersAsync(string playerCode);

	ValueTask<int> QueryStationLettersTotalCountAsync(string playerCode);

	ValueTask SetStationLetterHaveReadAsync(string playerCode, Guid letterId);
}
