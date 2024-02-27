using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TGS.Core.DataServices.Tgs;

namespace TGS.Services.Operations.Queries;

public interface IGameQueries
{
	Task<Dictionary<int, string>> GetGamesLocalizationByGameIdsAsync(int[] gameIds);
}

public sealed class GameQueries(ITgsRepository repository) : IGameQueries
{
	Task<Dictionary<int, string>> IGameQueries.GetGamesLocalizationByGameIdsAsync(int[] gameIds)
	{
		return repository.Games
			.Where(x => gameIds.Contains(x.Id)
				&& x.Localization != null)
			.ToDictionaryAsync(x => x.Id, x => x.Localization);
	}
}