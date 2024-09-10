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
using TGS.Services.Operator.ApplicationServices.Category;

namespace TGS.Apis.OperatorApi.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class CategoryController : ControllerBase
	{
		/// <summary>
		/// Get Categories
		/// </summary>
		[HttpPost]
		[AuthorizePlayerToken(AllowNullOrEmpty = true, NeedAuthorize = false)]
		public async IAsyncEnumerable<CategoryResponse> GetGamesAsync(
			[FromServices] IBrandLanguageCodeMapper brandLanguageCodeMapper,
			[FromServices] IGetService<GetCategoriesQuery, IEnumerable<Category>> queryAllGameService,
			[FromServices] IPlayerOperations playerOperations,
			[FromBody] GetCategoriesRequest request)
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

			var query = new GetCategoriesQuery(
				brandId,
				playerType,
				platformType,
				cultureCode,
				request.PageSize,
				request.PageIndex,
				request.GameTypes,
				request.Tag);

			var categories = await queryAllGameService.GetAsync(query);

			foreach (var category in categories)
			{
				yield return new CategoryResponse
				{
					PageSize = query.PageSize,
					PageIndex = query.PageIndex,
					Total = category.Total,
					CategoryName = category.CategoryName,
					GameType = category.GameType,
					Games = category.Games.Select(game => new GameModel
					{
						Id = game.Id,
						Code = game.Code,
						Name = game.Name,
						Description = game.Description,
						GameProviderCode = game.GameProviderCode,
						IsActive = game.IsActive,
						Type = game.Type,
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
	}
}
