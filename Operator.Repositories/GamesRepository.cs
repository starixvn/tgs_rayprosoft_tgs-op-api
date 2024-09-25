using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Castle.Core.Internal;
using Microsoft.EntityFrameworkCore;
using TGS.Core.DataServices.Tgs;
using TGS.Core.DataServices.Tgs.Models;
using TGS.Core.Models.Enums;
using TGS.Operator.Repositories.Extensions;
using TGS.Operator.Repositories.Models;
using TGS.Operator.Repositories.Services;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Game;
using TGS.Services.Operator.GameProviders;
using GetGamesQuery = TGS.Services.Operator.ApplicationServices.GameTypes.GetGamesQuery;

namespace TGS.Operator.Repositories
{
	public class GamesRepository(ITgsRepository tgsRepository) : IGamesRepository
	{
		private readonly ITgsRepository _tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));

		private readonly Status[] _providerStatuses = { Status.Active, Status.Maintenance };

		private readonly GameStatus[] _statuses = { GameStatus.Active, GameStatus.Maintenance, GameStatus.Testing };

		public async Task<IEnumerable<GamesModel>> QueryGameByGameTypesAsync(GetGamesQuery query, GamePlatformType[] platformTypes)
		{
			var brand = await _tgsRepository.Brands.FirstOrDefaultAsync(b => b.Id == query.BrandId);
			if (brand is not { Status: BrandStatus.Active })
			{
				return Enumerable.Empty<GamesModel>();
			}

			var providersAvailableForBrand = await GetProviderActiveForBrand(query.BrandId, query.ProviderCode);
			var groupedGamesQuery = GetAndGroupGames(query, platformTypes, providersAvailableForBrand);
			var paginatedResults = GameService.OrderAndPagination(groupedGamesQuery, query.OrderType, query.PageIndex, query.PageSize);
			var providerDictionary = providersAvailableForBrand.ToDictionary(bp => bp.GameProviderId, bp => bp.Status);

			return paginatedResults.ToList().Select(info => new GamesModel
			{
				Total = info.Total,
				Key = info.Key,
				Games = GameService.MapCategory(info.Games, query.CultureCode, providerDictionary)
			});
		}

		public async Task<IEnumerable<GameTagInfo>> QueryGameByTagsAsync(GetTopGamesByTagQuery query, GamePlatformType[] gamePlatformTypes)
		{
			var brand = await _tgsRepository.Brands.FirstOrDefaultAsync(b => b.Id == query.BrandId);
			if (brand is not { Status: BrandStatus.Active })
			{
				return Enumerable.Empty<GameTagInfo>();
			}

			var providersActivated = await GetProviderActiveForBrand(query.BrandId, query.ProviderCode);

			var providersAvailable = providersActivated.Select(bp => bp.GameProviderId);

			var gamesQuery = _tgsRepository.BrandGames
				.Where(bg => bg.BrandId == query.BrandId &&
							 bg.Game.AvailableToBrand &&
							 providersAvailable.Contains(bg.Game.GameProviderId) &&
							 gamePlatformTypes.Contains(bg.Game.PlatformType) &&
							 _providerStatuses.Contains(bg.Game.GameProvider.Status) &&
							 _statuses.Contains(bg.Status) &&
							 _statuses.Contains(bg.Game.Status) &&
							 !query.GameTypeExclude.Contains(bg.Game.Type))

				.SelectGameInfo()
				.AsNoTracking();

			return await GetGroupGameTag(query, gamesQuery, providersActivated);
		}

		private IQueryable<IGrouping<object, GameDto>> GetAndGroupGames(GetGamesQuery query, GamePlatformType[] platformTypes, IEnumerable<BrandGameProvider> gameProviders)
		{
			var providersAvailable = gameProviders.Select(bp => bp.GameProviderId);
			var tagQuery = query.Tag.Sum(t => (byte)t);

			return _tgsRepository.BrandGames
				.Where(bg => bg.BrandId == query.BrandId &&
							 bg.Game.AvailableToBrand &&
							 providersAvailable.Contains(bg.Game.GameProviderId) &&
							 platformTypes.Contains(bg.Game.PlatformType) &&
							 _providerStatuses.Contains(bg.Game.GameProvider.Status) &&
							 _statuses.Contains(bg.Status) &&
							 _statuses.Contains(bg.Game.Status) &&
							 (query.Types.IsNullOrEmpty() || query.Types.Contains(bg.Game.Type)) &&
							 (query.Tag.IsNullOrEmpty() || (tagQuery & (byte)bg.Tag) > 0))
				.SelectGameInfo()
				.GroupByGameQuery(
					(query.Types, bg => bg.Type),
					(query.ProviderCode, bg => bg.GameProviderCode))
				.AsNoTracking();
		}

		private async Task<IEnumerable<GameTagInfo>> GetGroupGameTag(GetTopGamesByTagQuery query,
			IQueryable<GameDto> gamesQuery, IEnumerable<BrandGameProvider> providersActivated)
		{
			var groupedGames = new List<GameTagInfo>();

			foreach (var tag in query.Tags)
			{
				var tagQuery = gamesQuery
					.Where(g => (g.Tag & tag) != 0);

				var games = await tagQuery.OrderBy(g => g.DisplayOrder)
				.ThenBy(g => g.Code)
				.Skip((query.PageIndex - 1) * query.PageSize).Take(query.PageSize)
				.ToListAsync();
				var gameCount = await tagQuery.CountAsync();

				groupedGames.Add(new GameTagInfo
				{
					Tag = tag,
					Total = gameCount,
					Games = GameService.MapCategory(games, query.CultureCode, providersActivated.ToDictionary(bp => bp.GameProviderId, bp => bp.Status))
				});
			}

			return groupedGames;
		}

		private async Task<IEnumerable<BrandGameProvider>> GetProviderActiveForBrand(Guid brandId, string[] providerCodes)
		{
			return await _tgsRepository.BrandGameProviders
				.Where(bp => bp.BrandId == brandId && _providerStatuses.Contains(bp.Status))
				.Where(bp => providerCodes.IsNullOrEmpty() || providerCodes.Contains(bp.GameProvider.Code))
				.ToListAsync();
		}
	}
}
