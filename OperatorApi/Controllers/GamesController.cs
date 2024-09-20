using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Core.Models.Enums;
using TGS.Services.Operations;
using TGS.Services.Operations.CodeMappers;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Game;
using GetGamesQuery = TGS.Services.Operator.ApplicationServices.GameTypes.GetGamesQuery;

namespace TGS.Apis.OperatorApi.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class GamesController : ControllerBase
	{
		/// <summary>
		/// Get Categories
		/// </summary>
		[HttpPost]
		[AuthorizePlayerToken(AllowNullOrEmpty = true, NeedAuthorize = false)]
		public async IAsyncEnumerable<GamesResponse> GetGamesAsync(
			[FromServices] IBrandLanguageCodeMapper brandLanguageCodeMapper,
			[FromServices] IGetService<GetGamesQuery, IEnumerable<GamesModel>> queryAllGameService,
			[FromServices] IPlayerOperations playerOperations,
			[FromBody] GetGameTypesRequest request)
		{
			var brandId = HttpContext.Items.GetBrandId();
			var playerTokenData = HttpContext.Items.GetPlayerTokenData();

			var playerType = PlayerType.RealPlayer;
			var platformType = request.PlatformType;

			if (playerTokenData != null)
			{
				var cachedPlayer = await playerOperations
					.GetCachedPlayerAsync(playerTokenData.PlayerCode)
					.ConfigureAwait(false);

				playerType = cachedPlayer.Type;
				platformType = playerTokenData.PlatformType;
			}

			var currentCultureInfo = CultureInfo.CurrentUICulture;

			var cultureCode = await brandLanguageCodeMapper
				.ExternalToInternalAsync(currentCultureInfo.Name, brandId)
				.ConfigureAwait(false);

			var query = new GetGamesQuery(
				brandId,
				playerType,
				platformType,
				cultureCode,
				request.PageSize,
				request.PageIndex,
				request.ProviderCode,
				request.GameTypes,
				request.Tag,
				request.OrderType);

			var gameGrouped = await queryAllGameService.GetAsync(query);

			foreach (var group in gameGrouped)
			{
				yield return new GamesResponse
				{
					PageSize = query.PageSize,
					PageIndex = query.PageIndex,
					Total = group.Total,
					Key = group.Key,
					Games = group.Games.Select(game => new GameModel
					{
						Id = game.Id,
						Code = game.Code,
						Name = game.Name,
						Description = game.Description,
						GameProviderCode = game.GameProviderCode,
						GameProviderName = game.GameProviderName,
						SubGameProviderCode = game.SubGameProviderCode,
						Type = game.Type,
						GameStatus = game.GameStatus,
						Tag = new Tag
						{
							Hot = game.Tag.Hot,
							New = game.Tag.New,
							Recommend = game.Tag.Recommend,
						}

					})
				};
			}
		}

		[HttpPost("by-tag")]
		[AuthorizePlayerToken(AllowNullOrEmpty = true, NeedAuthorize = false)]
		public async IAsyncEnumerable<GameTagModel> GetTopGamesByTypesAsync(
			[FromServices] IBrandLanguageCodeMapper brandLanguageCodeMapper,
			[FromServices] IGetService<GetTopGamesByTagQuery, IEnumerable<GameTagInfo>> queryAllGameService,
			[FromBody] GetTopGamesByTagRequest request)
		{
			var brandId = HttpContext.Items.GetBrandId();
			var playerTokenData = HttpContext.Items.GetPlayerTokenData();
			var platformType = request.PlatformType;

			if (playerTokenData != null)
			{
				platformType = playerTokenData.PlatformType;
			}

			var currentCultureInfo = CultureInfo.CurrentUICulture;

			var cultureCode = await brandLanguageCodeMapper
				.ExternalToInternalAsync(currentCultureInfo.Name, brandId)
				.ConfigureAwait(false);


			var query = new GetTopGamesByTagQuery(
				brandId,
				platformType,
				cultureCode,
				request.PageIndex,
				request.PageSize,
				request.Tags,
				request.ProviderCode);

			var gameTagInfos = await queryAllGameService.GetAsync(query);


			foreach (var info in gameTagInfos)
			{
				yield return new GameTagModel
				{
					Tag = info.Tag,
					PageSize = query.PageSize,
					PageIndex = query.PageIndex,
					Total = info.Total,
					Games = info.Games.Select(game => new GameModel
					{
						Code = game.Code,
						GameProviderCode = game.GameProviderCode,
						GameProviderName = game.GameProviderName,
						SubGameProviderCode = game.SubGameProviderCode,
						Description = game.Description,
						DisplayOrder = game.DisplayOrder,
						Id = game.Id,
						Name = game.Name,
						Tag = new Tag { Hot = game.Tag.Hot, New = game.Tag.New, Recommend = game.Tag.Recommend },
						Type = game.Type,
						GameStatus = game.GameStatus
					})
				};
			}
		}
	}
}
