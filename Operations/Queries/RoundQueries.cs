using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TGS.Core.DataServices.Tgs;
using TGS.Core.DataServices.Tgs.Models;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Services.Data;

namespace TGS.Services.Operations.Queries;

public interface IRoundQueries
{
	ValueTask<Round> GetRoundByRoundIdAsync(Guid roundId, bool includeGameActions = false,
		bool includeRoundDetail = false);

	ValueTask<(RoundStatus RoundStatus, int GameId)?> GetRoundStatusAndGameIdAsync(Guid gameProviderId,
		string playerCode, string externalRoundId, string externalTransactionId);

	ValueTask<RoundStatus?> GetRoundStatusAsync(Guid gameProviderId, string playerCode, string externalRoundId,
		int gameId);

	ValueTask<RoundWithGameActionExternalIds[]> GetRoundWithGameActionExternalIdsAsync(Guid gameProviderId,
				DateTimeOffset start, DateTimeOffset end, RoundStatus? roundStatus = RoundStatus.Closed,
		GameType? gameType = null);
}

public sealed class RoundQueries(ITgsRepository repository) : IRoundQueries
{
	async ValueTask<Round> IRoundQueries.GetRoundByRoundIdAsync(Guid roundId, bool includeGameActions,
		bool includeRoundDetail)
	{
		IQueryable<Round> query = repository.Rounds.Include(r => r.RoundDetail);

		if (includeGameActions)
		{
			query = query.Include(c => c.GameActions);
		}

		if (includeRoundDetail)
		{
			query = query.Include(c => c.RoundDetail);
		}

		return await query.FirstOrDefaultAsync(c => c.Id == roundId).ConfigureAwait(false);
	}

	async ValueTask<(RoundStatus RoundStatus, int GameId)?> IRoundQueries.
		GetRoundStatusAndGameIdAsync(
		Guid gameProviderId,
		string playerCode,
		string externalRoundId,
		string externalTransactionId)
	{
		var data = await (from ga in repository.GameActions
						  let r = ga.Round
						  where r.GameProviderId == gameProviderId &&
							  r.PlayerCode == playerCode &&
							  r.ExternalRoundId == externalRoundId &&
							  ga.ExternalTransactionId == externalTransactionId
						  select new
						  {
							  r.Status,
							  r.GameId
						  }).FirstOrDefaultAsync().ConfigureAwait(false);

		return data == null ? null : (data.Status, data.GameId);
	}

	async ValueTask<RoundStatus?> IRoundQueries.GetRoundStatusAsync(Guid gameProviderId, string playerCode,
		string externalRoundId, int gameId) =>
		await (from round in repository.Rounds
			   where round.GameProviderId == gameProviderId &&
				   round.PlayerCode == playerCode &&
				   round.ExternalRoundId == externalRoundId &&
				   round.GameId == gameId
			   select (RoundStatus?)round.Status)
			.FirstOrDefaultAsync().ConfigureAwait(false);

	async ValueTask<RoundWithGameActionExternalIds[]> IRoundQueries.GetRoundWithGameActionExternalIdsAsync(
				Guid gameProviderId, DateTimeOffset start, DateTimeOffset end, RoundStatus? roundStatus, GameType? gameType)
	{
		var query = repository.Rounds
			.Where(r => r.GameProviderId == gameProviderId &&
				r.CreatedOn >= start && r.CreatedOn <= end &&
				r.Status == roundStatus)
			.Include(r => r.GameActions)
			.Join(repository.Games, r => r.GameId, g => g.Id, (r, g) => new
			{
				Round = r,
				Game = g
			});

		if (gameType.HasValue)
		{
			query = query.Where(a => a.Game.Type == gameType);
		}

		return await query.Select(a => new RoundWithGameActionExternalIds
		{
			Id = a.Round.Id,
			PlayerCode = a.Round.PlayerCode,
			CurrencyCode = a.Round.CurrencyCode,
			ExternalRoundId = a.Round.ExternalRoundId,
			GameId = a.Game.Id,
			ExternalGameId = a.Game.ExternalId,
			Status = a.Round.Status,
			ExternalCreatedOn = a.Round.ExternalCreatedOn ?? a.Round.CreatedOn,
			GameActionExternalIds = a.Round.GameActions.Select(ga => new GameActionExternalIds
			{
				ExternalGameActionGroupId = ga.ExternalGameActionGroupId,
				ExternalBetId = ga.ExternalBetId,
				ExternalTransactionId = ga.ExternalTransactionId,
				ExternalReferencedTransactionId =
					a.Round.GameActions.Where(act => act.Id == ga.ReferencedGameActionId)
						.Select(act => act.ExternalTransactionId).FirstOrDefault(),
				ExternalRoundId = a.Round.ExternalRoundId,
				Amount = ga.Amount,
				GameActionType = ga.Type,
				RecordedOn = ga.RecordedOn
			}).ToArray()
		}).ToArrayAsync().ConfigureAwait(false);
	}
}
