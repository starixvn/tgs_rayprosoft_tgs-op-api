using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using Tgs.ServiceUtility;
using TGS.Core.Infrastructures.Extensions;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Exceptions;
using TGS.Core.Models.Extensions;
using TGS.Core.Models.Services.Caches;
using TGS.Services.Operations;
using TGS.Services.Operator.GameProviders;

namespace TGS.Services.Operator.ApplicationServices.GameProvider;

internal class GetGameProvidersQueryService(
	IPlayerOperations playerOperations,
	IGameProviderRepository gameProviderRepository) : IQueryService<GetGameProvidersQuery, GameProviderInfo>
{
	private readonly IGameProviderRepository _gameProviderRepository =
			gameProviderRepository ?? throw new ArgumentNullException(nameof(gameProviderRepository));
	private readonly IPlayerOperations _playerOperations = playerOperations ?? throw new ArgumentNullException(nameof(playerOperations));

	public async IAsyncEnumerable<GameProviderInfo> QueryAsync(
		GetGameProvidersQuery query,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var brandId = query.BrandId;

		var playerTokenData = query.PlayerTokenData;
		var platformType = query.PlatformType;

		CachedPlayer? cachedPlayer = null;

		if (playerTokenData != null)
		{
			if (playerTokenData.PlatformType != platformType)
			{
				throw new InvalidOperationException("request platform is not matched with player login platform.");
			}

			cachedPlayer = await _playerOperations.GetCachedPlayerAsync(playerTokenData.PlayerCode);

			if (cachedPlayer != null
				&& cachedPlayer.DefaultWalletCurrencyCode.IgnoreCaseNotEquals(query.CurrencyCode))
			{
				throw new CurrencyNotMatchingException(cachedPlayer.DefaultWalletCurrencyCode, query.CurrencyCode);
			}
		}

		var playerType = cachedPlayer?.Type ?? PlayerType.RealPlayer;

		var statuses = GetAvailableStatus(playerType);
		var gameStatuses = GetAvailableGameStatus(playerType);

		var gamePlatformTypes = platformType.ToGamePlatformTypes();

		var brandSubGps = await _gameProviderRepository
			.QueryAllSubGameProvidersAsync(query.BrandId)
			.Where(sgp => statuses.Contains(sgp.Status))
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		var brandGames = await _gameProviderRepository.QueryAllGamesAsync(brandId)
			.Where(g => gamePlatformTypes.Contains(g.PlatformType))
			.Where(g => g.AvailableToBrand)
			.Where(g => gameStatuses.Contains(g.Status))
			.ToArrayAsync(cancellationToken).ConfigureAwait(false);

		var gameTypeInfos = (from game in brandGames
							 where game.AvailableToBrand
							 group game.Type by game.SubGpCode
							 into g
							 select g)
			.ToDictionary(x => x.Key, x => x.Distinct().ToArray());

		foreach (var sgp in brandSubGps)
		{
			if (!gameTypeInfos.TryGetValue(sgp.Code, out var gameTypes) || gameTypes.Length == 0)
			{
				continue;
			}

			yield return new GameProviderInfo
			{
				Code = sgp.Code,
				Name = sgp.Name,
				Description = sgp.Description,
				Status = sgp.Status,
				IsTransferWallet = sgp.IsTransferWallet,
				GameTypes = gameTypes,
				Localization = string.IsNullOrEmpty(sgp.Localization)
					? []
					: JsonSerializer.Deserialize<Dictionary<string, JsonDocument>>(
						sgp.Localization)
			};

			if (cancellationToken.IsCancellationRequested)
			{
				yield break;
			}
		}
	}

	private static GameStatus[] GetAvailableGameStatus(PlayerType playerType)
		=> playerType switch
		{
			PlayerType.RealPlayer => [
				GameStatus.Active,
				GameStatus.Maintenance
			],
			PlayerType.TestPlayer => [
				GameStatus.Active,
				GameStatus.Maintenance,
				GameStatus.Testing
			],
			PlayerType.All => [
				GameStatus.Active,
				GameStatus.Maintenance,
				GameStatus.Testing
			],
			_ => Array.Empty<GameStatus>()
		};

	private static Status[] GetAvailableStatus(PlayerType playerType)
	{
		var statuses = new[]
		{
			Status.Active,
			Status.Maintenance
		};

		return playerType switch
		{
			PlayerType.RealPlayer => statuses,
			PlayerType.TestPlayer => statuses,
			PlayerType.All => statuses,
			_ => []
		};
	}
}
