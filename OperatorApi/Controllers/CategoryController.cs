using Microsoft.AspNetCore.Mvc;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Core.Models.Enums;
using TGS.Services.Operations;
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
		[HttpGet]
		[AuthorizePlayerToken(AllowNullOrEmpty = true, NeedAuthorize = false)]
		public async IAsyncEnumerable<CategoryResponse> GetGamesAsync(
			[FromServices] IGetService<GetCategoriesQuery, IEnumerable<Category>> queryAllGameService,
			[FromServices] IPlayerOperations playerOperations,
			[FromQuery] GetCategoriesRequest request)
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

			var query = new GetCategoriesQuery(
				brandId,
				playerType,
				platformType,
				request.Type ?? 0,
				request.PageSize,
				request.PageIndex);

			var categories = await queryAllGameService.GetAsync(query);

			foreach (var category in categories)
			{
				yield return new CategoryResponse
				{
					PageSize = category.PageSize,
					PageIndex = category.PageIndex,
					Total = category.Total,
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
