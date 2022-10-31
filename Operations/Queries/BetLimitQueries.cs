using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TGS.Core.DataServices.Tgs;
using TGS.Core.Models.Services.Data;

namespace TGS.Services.Operations.Queries;

public interface IBetLimitQueries
{
	Task<SportBetLimitResult[]> GetPlayerSportBetLimitsAsync(string playerCode, Guid gameProviderId,
		Guid[] sportMarketTypeIds);
}

public sealed class BetLimitQueries : IBetLimitQueries
{
	private readonly ITgsRepository _repository;

	public BetLimitQueries(ITgsRepository repository)
	{
		_repository = repository;
	}

	Task<SportBetLimitResult[]> IBetLimitQueries.GetPlayerSportBetLimitsAsync(string playerCode,
		Guid gameProviderId, Guid[] sportMarketTypeIds) =>
		(from b in _repository.PlayerSportsBetLimits
		 where b.PlayerCode == playerCode && sportMarketTypeIds.Contains(b.SportMarketTypeId)
		 join sm in _repository.SportMarketTypes on b.SportMarketTypeId equals sm.Id
		 join s in _repository.SportTypes on sm.SportTypeId equals s.Id
		 join m in _repository.MarketTypes on sm.MarketTypeId equals m.Id
		 where s.GameProviderId == gameProviderId && m.GameProviderId == gameProviderId
		 select new SportBetLimitResult
		 {
			 SportTypeExternalId = s.ExternalId,
			 MarketTypeExternalId = m.ExternalId,
			 MinBet = b.MinBet,
			 MaxBet = b.MaxBet,
			 MaxBetPerMatch = b.MaxBetPerMatch
		 }).ToArrayAsync();
}
