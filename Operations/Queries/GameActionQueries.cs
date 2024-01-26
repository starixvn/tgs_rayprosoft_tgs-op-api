using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TGS.Core.DataServices.Base.CustomQueryParameters;
using TGS.Core.DataServices.Base.Extensions;
using TGS.Core.DataServices.Tgs;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Services.Data;

namespace TGS.Services.Operations.Queries;

public interface IGameActionQueries
{
	ValueTask<GameActionExternalIds> GetGameActionExternalIdAsync(Guid gameProviderId, string playerCode,
		string externalTransactionId);

	ValueTask<GameActionExternalIds[]> GetGameActionExternalIdsAsync(Guid gameProviderId, string playerCode,
		string externalRoundId, int gameId, GameActionType[] gameActionTypes);

	ValueTask<GameActionExternalIds[]> GetGameActionExternalIdsByExternalGameActionGroupIdAsync(
		Guid gameProviderId, string playerCode, string externalGameActionGroupId);

	ValueTask<string[]> GetGameActionExternalIdsByExternalGameActionGroupIdAsync(Guid gameProviderId,
		string playerCode, string externalRoundId, int gameId, string externalGameActionGroupId);

	ValueTask<GameActionData> GetGameActionsByExternalRoundIdAsync(Guid gameProviderId, string playerCode,
		string externalRoundId, int gameId);

	ValueTask<GameActionExternalIds[]> GetValidGameActionExternalIdsAsync(Guid gameProviderId,
							string playerCode, string externalRoundId, int gameId, bool onlyPlacements);

	ValueTask<bool> ReferencedGameActionExistsAsync(Guid gameProviderId, string playerCode,
		string externalRoundId, string externalTransactionId);
}

public sealed class GameActionQueries(ITgsRepository repository) : IGameActionQueries
{
	async ValueTask<GameActionExternalIds> IGameActionQueries.GetGameActionExternalIdAsync(
		Guid gameProviderId,
		string playerCode,
		string externalTransactionId)
	{
		var gameActionData =
			await (from round in repository.Rounds.AsQueryable()
				   join gameAction in repository.GameActions.AsQueryable()
				   on round.Id equals gameAction.RoundId
				   join game in repository.Games.AsQueryable()
				   on round.GameId equals game.Id
				   where round.GameProviderId == gameProviderId
				   && round.PlayerCode == playerCode
				   && gameAction.ExternalTransactionId == externalTransactionId
				   select new
				   {
					   gameAction.ExternalBetId,
					   gameAction.ExternalTransactionId,
					   round.ExternalRoundId,
					   gameAction.ExternalGameActionGroupId,
					   gameAction.ReferencedGameActionId,
					   round.GameId,
					   ExternalGameId = game.ExternalId,
					   gameAction.Amount,
					   gameAction.RecordedOn,
					   GameActionType = gameAction.Type
				   }).FirstOrDefaultAsync();

		if (gameActionData == null)
		{
			return null;
		}

		var externalReferencedTransactionId = default(string);

		if (gameActionData.ReferencedGameActionId.HasValue)
		{
			externalReferencedTransactionId = await repository.GameActions.AsQueryable()
			   .Where(x => x.Id == gameActionData.ReferencedGameActionId.Value)
			   .Select(x => x.ExternalTransactionId).FirstOrDefaultAsync();
		}

		return new()
		{
			ExternalBetId = gameActionData.ExternalBetId,
			ExternalTransactionId = gameActionData.ExternalTransactionId,
			ExternalRoundId = gameActionData.ExternalRoundId,
			ExternalGameActionGroupId = gameActionData.ExternalGameActionGroupId,
			ExternalReferencedTransactionId = externalReferencedTransactionId,
			ExternalGameId = gameActionData.ExternalGameId,
			Amount = gameActionData.Amount,
			RecordedOn = gameActionData.RecordedOn,
			GameActionType = gameActionData.GameActionType,
		};
	}

	async ValueTask<GameActionExternalIds[]> IGameActionQueries.GetGameActionExternalIdsAsync(Guid gameProviderId,
		string playerCode, string externalRoundId, int gameId, GameActionType[] gameActionTypes)
	{
		const string spName = "bet.game_action_get_game_action_external_ids";
		var r = await repository.SqlQueryMultipleAsync<GameActionExternalIds>(spName, parameters:
			new Dictionary<string, object>
			{
				{":game_provider_id", gameProviderId},
				{":player_code", playerCode},
				{":external_round_id", externalRoundId},
				{":game_id", gameId},
				{":game_action_types", new ArrayParameter<int>(gameActionTypes.Select(s => (int)s).ToArray())}
			});

		return r.ToArray();
	}

	async ValueTask<GameActionExternalIds[]> IGameActionQueries.
		GetGameActionExternalIdsByExternalGameActionGroupIdAsync(
			Guid gameProviderId, string playerCode, string externalGameActionGroupId)
	{
		const string spName = "bet.game_action_get_game_actions_by_external_group_id";
		var r = await repository.SqlQueryMultipleAsync<GameActionExternalIds>(spName, parameters:
			new Dictionary<string, object>
			{
				{":game_provider_id", gameProviderId},
				{":player_code", playerCode},
				{":external_group_id", externalGameActionGroupId}
			});

		return r.ToArray();
	}

	async ValueTask<string[]> IGameActionQueries.GetGameActionExternalIdsByExternalGameActionGroupIdAsync(
		Guid gameProviderId, string playerCode, string externalRoundId, int gameId,
		string externalGameActionGroupId) =>
		await (from ga in repository.GameActions
			   let round = ga.Round
			   where round.ExternalRoundId == externalRoundId &&
				   round.GameProviderId == gameProviderId &&
				   round.PlayerCode == playerCode &&
				   round.GameId == gameId &&
				   ga.ExternalGameActionGroupId == externalGameActionGroupId
			   select ga.ExternalTransactionId).ToArrayAsync()
			.ConfigureAwait(false);

	async ValueTask<GameActionData> IGameActionQueries.GetGameActionsByExternalRoundIdAsync(Guid gameProviderId,
		string playerCode, string externalRoundId, int gameId)
	{
		const string spName = "bet.game_action_get_game_actions_by_external_round_id";
		var r = await repository.SqlQueryMultipleAsync<GameActionResult>(spName, parameters:
			new Dictionary<string, object>
			{
				{":game_provider_id", gameProviderId},
				{":player_code", playerCode},
				{":external_round_id", externalRoundId},
				{":game_id", gameId}
			});

		return !r.Any()
			? null
			: r.ToLookup(o => new
			{
				Id = o.RoundId,
				o.ExternalRoundId,
				Status = o.RoundStatus,
				BetAmount = o.RoundBetAmount,
				ValidBetAmount = o.RoundValidBetAmount,
				WinLoss = o.RoundWinLoss,
				o.Commission,
				o.TieBetAdjustment,
				o.PlatformType,
				ClosedOn = o.RoundClosedOn
			})
			.Select(g => new GameActionData
			{
				Round = new()
				{
					Id = g.Key.Id,
					ExternalRoundId = g.Key.ExternalRoundId,
					Status = g.Key.Status,
					BetAmount = g.Key.BetAmount,
					ValidBetAmount = g.Key.ValidBetAmount,
					WinLoss = g.Key.WinLoss,
					Commission = g.Key.Commission,
					TieBetAdjustment = g.Key.TieBetAdjustment,
					PlatformType = g.Key.PlatformType,
					ClosedOn = g.Key.ClosedOn
				},
				GameActions = g.Select(gar => new GameActionRecord
				{
					Id = gar.Id,
					ExternalTransactionId = gar.ExternalTransactionId,
					ExternalBetId = gar.ExternalBetId,
					ExternalGameActionGroupId = gar.ExternalGameActionGroupId,
					Amount = gar.Amount,
					Type = gar.GameActionType,
					RecordedOn = gar.RecordedOn,
					ReferencedGameActionId = gar.ReferencedGameActionId
				}).ToArray()
			}).First();
	}

	async ValueTask<GameActionExternalIds[]> IGameActionQueries.GetValidGameActionExternalIdsAsync(
							Guid gameProviderId, string playerCode, string externalRoundId, int gameId, bool onlyPlacements)
	{
		const string spName = "bet.game_action_get_valid_game_action_external_ids";
		var r = await repository.SqlQueryMultipleAsync<GameActionExternalIds>(spName,
			parameters: new Dictionary<string, object>
			{
				{":game_provider_id", gameProviderId},
				{":player_code", playerCode},
				{":external_round_id", externalRoundId},
				{":game_id", gameId},
				{":only_placements", onlyPlacements}
			});

		return r.ToArray();
	}

	async ValueTask<bool> IGameActionQueries.ReferencedGameActionExistsAsync(Guid gameProviderId, string playerCode,
		string externalRoundId, string externalTransactionId)
	{
		const string spName = "bet.game_action_referenced_game_action_exists";
		return await repository.SqlQuerySingleAsync<bool>(spName, parameters:
			new Dictionary<string, object>
			{
				{":game_provider_id", gameProviderId},
				{":player_code", playerCode},
				{":external_round_id", externalRoundId},
				{":external_transaction_id", externalTransactionId}
			});
	}
}
