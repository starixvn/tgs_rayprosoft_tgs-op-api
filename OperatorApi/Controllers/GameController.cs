using System.Globalization;
using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Core.Models.Enums;
using TGS.Services.Operations;
using TGS.Services.Operations.CodeMappers;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Game;
using TGS.Services.Operator.ApplicationServices.GameBalance;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
public class GameController : ControllerBase
{
	[HttpGet("bc")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async ValueTask<ContentResult> BCSportsHtmlAsync(
		[FromServices] ICommandService<FundInAllGameBalanceCommand> fundInAllService,
		[FromServices] IGetService<GetBetConstructHtmlContentQuery, BetConstructHtmlContentInfo> getService)
	{
		var currentCultureName = CultureInfo.CurrentUICulture.Name;

		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.Items.GetBrandId();
		var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

		await fundInAllService
			.ExecuteAsync(new FundInAllGameBalanceCommand(
				brandId,
				playerTokenData.PlayerCode,
				null))
			.ConfigureAwait(false);

		var query = new GetBetConstructHtmlContentQuery(playerTokenData, brandId, ip, currentCultureName);

		var result = await getService.GetAsync(query).ConfigureAwait(false);

		return new ContentResult
		{
			Content = result.Html,
			ContentType = "text/html",
			StatusCode = StatusCodes.Status200OK
		};
	}

	[HttpGet("favorite/{gameType}")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public IAsyncEnumerable<int> GetFavoriteGamesAsync(
				[FromServices] IQueryService<ListFavoriteGameIdsQuery, FavoriteGameInfo> listService,
				GameType gameType)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.Items.GetBrandId();

		var query = new ListFavoriteGameIdsQuery(
			brandId,
			playerTokenData.PlayerCode,
			gameType);

		return listService.QueryAsync(query)
			.Select(fg => fg.GameId);
	}

	/// <summary>
	/// get game list of specific game provider
	/// </summary>
	[HttpGet("list")]
	[AuthorizePlayerToken(AllowNullOrEmpty = true, NeedAuthorize = false)]
	public async IAsyncEnumerable<GameModel> GetGamesAsync(
		[FromServices] IBrandLanguageCodeMapper brandLanguageCodeMapper,
		[FromServices] IQueryService<GetGamesQuery, GameInfo> getGamesQueryService,
		[FromServices] IQueryService<GetAllGamesByGameTypeQuery, GameInfo> queryAllGameService,
		[FromServices] IPlayerOperations playerOperations,
		[FromQuery] GetGamesRequest request)
	{
		var brandId = HttpContext.Items.GetBrandId();
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var currentCultureInfo = CultureInfo.CurrentUICulture;

		var cultureCode = await brandLanguageCodeMapper
			.ExternalToInternalAsync(currentCultureInfo.Name, brandId)
			.ConfigureAwait(false);

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

		if (string.IsNullOrWhiteSpace(request.SubGameProviderCode))
		{
			var query = new GetAllGamesByGameTypeQuery(
				brandId,
				playerType,
				platformType,
				request.Types ?? Array.Empty<GameType>());

			var games = from info in queryAllGameService.QueryAsync(query)
						let lang = info.Localization.TryGetValue(cultureCode, out var value)
							? value
							: JsonDocument.Parse("{}")
						orderby info.DisplayOrder, info.Code
						select new GameModel
						{
							Code = info.Code,
							GameProviderCode = info.GameProviderCode,
							Description = GetLocalization(
								lang.RootElement,
								"description",
								info.Description),
							DisplayOrder = info.DisplayOrder,
							Id = info.Id,
							IsActive = info.IsActive,
							Name = GetLocalization(
								lang.RootElement,
								"name",
								info.Name),
							Tag = new Tag
							{
								Hot = info.Tag.Hot,
								New = info.Tag.New,
								Recommend = info.Tag.Recommend
							},
							Type = info.Type
						};

			await foreach (var game in games)
				yield return game;
		}
		else
		{
			var query = new GetGamesQuery(
				brandId,
				request.SubGameProviderCode,
				playerType,
				platformType,
				request.Types ?? Array.Empty<GameType>());

			var games = from info in getGamesQueryService.QueryAsync(query)
						let lang = info.Localization.TryGetValue(cultureCode, out var value) 
							? value 
							: JsonDocument.Parse("{}")
						orderby info.DisplayOrder, info.Code
						select new GameModel
						{
							Code = info.Code,
							GameProviderCode = info.GameProviderCode,
							Description = GetLocalization(
								lang.RootElement,
								"description",
								info.Description),
							DisplayOrder = info.DisplayOrder,
							Id = info.Id,
							IsActive = info.IsActive,
							Name = GetLocalization(
								lang.RootElement,
								"name",
								info.Name),
							Tag = new Tag
							{
								Hot = info.Tag.Hot,
								New = info.Tag.New,
								Recommend = info.Tag.Recommend
							},
							Type = info.Type
						};

			await foreach (var game in games)
				yield return game;
		}
	}

	/// <summary>
	/// get game launch url via game id
	/// </summary>
	[HttpPost("geturl")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	[EnableUserBlockedCheck]
	public async Task<GetGameUrlResponse> GetGameUrlAsync(
		[FromServices] IGetService<GetGpCodeQuery, BrandSubGameProviderInfo> getGpCodeService,
		[FromServices] IGetService<GetGameUrlQuery, Uri> getGameUrlQueryService,
		[FromServices] ICommandService<FundInAllGameBalanceCommand> fundInAllService,
		[FromServices] ICommandService<FundOutBeforeEnterGameCommand> fundService,
		[FromBody] GetGameUrlRequest request)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.Items.GetBrandId();

		var subGp = await getGpCodeService
			.GetAsync(new GetGpCodeQuery(brandId, request.SubGameProviderCode))
			.ConfigureAwait(false);

		await fundInAllService
			.ExecuteAsync(new FundInAllGameBalanceCommand(
				brandId,
				playerTokenData.PlayerCode,
				subGp.GpCode))
			.ConfigureAwait(false);

		var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

		var query = new GetGameUrlQuery(
			brandId,
			playerTokenData,
			request.GameId,
			CultureInfo.CurrentUICulture.Name,
			ipAddress,
			request.ExtendedData);

		var gameUrl = await getGameUrlQueryService
			.GetAsync(query)
			.ConfigureAwait(false);

		if (subGp.IsTransferWallet)
		{
			await fundService
				.ExecuteAsync(new FundOutBeforeEnterGameCommand(
					brandId,
					playerTokenData.PlayerCode,
					subGp.GpCode))
				.ConfigureAwait(false);
		}

		return new GetGameUrlResponse
		{
			Url = gameUrl?.OriginalString
		};
	}

	/// <summary>
	/// get game launch url for lobby type game.
	/// </summary>
	[HttpPost("lobby/geturl")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	[EnableUserBlockedCheck]
	public async Task<GetGameUrlResponse> GetLobbyGameUrlAsync(
		[FromServices] IGetService<GetGpCodeQuery, BrandSubGameProviderInfo> getGpCodeService,
		[FromServices] IGetService<GetLobbyGameUrlQuery, Uri> getLobbyGameUrlQueryService,
		[FromServices] ICommandService<FundInAllGameBalanceCommand> fundInAllService,
		[FromServices] ICommandService<FundOutBeforeEnterGameCommand> fundService,
		[FromBody] GetLobbyGameUrlRequest request)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.Items.GetBrandId();

		var subGp = await getGpCodeService
			.GetAsync(new GetGpCodeQuery(brandId, request.SubGameProviderCode))
			.ConfigureAwait(false);

		var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

		await fundInAllService
			.ExecuteAsync(new FundInAllGameBalanceCommand(
				brandId,
				playerTokenData.PlayerCode,
				subGp.GpCode))
			.ConfigureAwait(false);

		var query = new GetLobbyGameUrlQuery(
			brandId,
			subGp.GameProviderId,
			playerTokenData,
			request.GameType,
			CultureInfo.CurrentUICulture.Name,
			ipAddress,
			request.Extends);

		var gameUrl = await getLobbyGameUrlQueryService
			.GetAsync(query)
			.ConfigureAwait(false);

		if (subGp.IsTransferWallet)
		{
			await fundService
				.ExecuteAsync(new FundOutBeforeEnterGameCommand(
					brandId,
					playerTokenData.PlayerCode,
					subGp.GpCode))
				.ConfigureAwait(false);
		}

		return new GetGameUrlResponse
		{
			Url = gameUrl?.OriginalString
		};
	}

	[HttpDelete("favorite/{gameId}")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public ValueTask RemoveGameFromFavoriteAsync(
		[FromServices] ICommandService<RemoveFavoriteGameCommand> removeService,
		int gameId)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var command = new RemoveFavoriteGameCommand(
			playerTokenData.PlayerCode,
			gameId);

		return removeService.ExecuteAsync(command);
	}

	[HttpPut("favorite/{gameId}")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async ValueTask SetGameToFavoriteAsync(
		[FromServices] ICommandService<SetFavoriteGameCommand> setService,
		[FromServices] IGetService<CountPlayerFavoriteGameQuery, int> countService,
		int gameId)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.Items.GetBrandId();

		var count = await countService
			.GetAsync(new CountPlayerFavoriteGameQuery(
				brandId,
				playerTokenData.PlayerCode))
			.ConfigureAwait(false);

		if (count >= 100)
			throw new FavoriteCountFullException();

		var command = new SetFavoriteGameCommand(
			brandId,
			playerTokenData.PlayerCode,
			gameId);

		await setService
			.ExecuteAsync(command)
			.ConfigureAwait(false);
	}

	[HttpGet("sm01")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async ValueTask<ContentResult> SM01HtmlAsync(
		[FromServices] ICommandService<FundInAllGameBalanceCommand> fundInAllService,
		[FromServices] IGetService<GetGpCodeQuery, BrandSubGameProviderInfo> getGpCodeService,
		[FromServices] ICommandService<FundOutBeforeEnterGameCommand> fundService,
		[FromServices] IGetService<GetSM01HtmlContentQuery, SM01HtmlContentInfo> getService)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.Items.GetBrandId();

		await fundInAllService
			.ExecuteAsync(new FundInAllGameBalanceCommand(
				brandId,
				playerTokenData.PlayerCode,
				null), HttpContext.RequestAborted)
			.ConfigureAwait(false);

		var subGp = await getGpCodeService
		.GetAsync(new GetGpCodeQuery(brandId, "SM01"))
		.ConfigureAwait(false);

		if (subGp.IsTransferWallet)
		{
			await fundService
				.ExecuteAsync(new FundOutBeforeEnterGameCommand(
					brandId,
					playerTokenData.PlayerCode,
					subGp.GpCode))
				.ConfigureAwait(false);
		}

		var query = new GetSM01HtmlContentQuery(playerTokenData.PlayerCode);

		var result = await getService
			.GetAsync(query, HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return new ContentResult
		{
			Content = result.Html,
			ContentType = "text/html",
			StatusCode = StatusCodes.Status200OK
		};
	}

	private static string GetLocalization(JsonElement element, string propertyName, string defaultValue)
	{
		if (!element.TryGetProperty(propertyName, out var value))
		{
			return defaultValue;
		}

		var val = value.GetString();
		return string.IsNullOrEmpty(val) ? defaultValue : val;
	}
}
