using System.Globalization;
using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Core.Models.Enums;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.GameProvider;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
public class GameProviderController : ControllerBase
{
	/// <summary>
	/// Get sub game provider list
	/// </summary>
	[HttpGet("list")]
	[AuthorizePlayerToken(AllowNullOrEmpty = true, NeedAuthorize = false)]
	public async IAsyncEnumerable<GameProviderModel> GetSubGameProvidersAsync(
		[FromServices] IQueryService<GetGameProvidersQuery, GameProviderInfo> getGameProviderQueryService,
		[FromQuery] GetGameProvidersRequest request)
	{
		var brandId = HttpContext.Items.GetBrandId();
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var currentCultureInfo = CultureInfo.CurrentUICulture;
		var cultureCode = currentCultureInfo.Name;

		var query = new GetGameProvidersQuery(
			brandId,
			request.CurrencyCode,
			playerTokenData,
			request.PlatformType);

		var infos = await getGameProviderQueryService
			.QueryAsync(query)
			.ToArrayAsync()
			.ConfigureAwait(false);

		foreach (var info in infos)
		{
			var lang = info.Localization.TryGetValue(cultureCode, out var value)
				? value
				: JsonDocument.Parse("{}");

			yield return new GameProviderModel
			{
				Code = info.Code,
				Description = GetLocalization(
					lang.RootElement,
					"description",
					info.Description),
				IsTransferWallet = info.IsTransferWallet,
				Name = GetLocalization(
					lang.RootElement,
					"name",
					info.Name),
				Status = info.Status,
				GameTypes = info.GameTypes,
			};
		}
	}


	/// <summary>
	/// Get game provider list
	/// </summary>
	[HttpGet]
	[AuthorizePlayerToken(AllowNullOrEmpty = true, NeedAuthorize = false)]
	public async IAsyncEnumerable<GameProviderModel> GetGameProvidersAsync(
		[FromServices] IGetService<GetGameProvidersQuery, IEnumerable<GameProviderInfo>> getGameProviderQueryService,
		[FromQuery] GetGameProvidersRequest request)
	{
		var brandId = HttpContext.Items.GetBrandId();
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var currentCultureInfo = CultureInfo.CurrentUICulture;
		var cultureCode = currentCultureInfo.Name;

		var query = new GetGameProvidersQuery(
			brandId,
			request.CurrencyCode,
			playerTokenData,
			request.PlatformType);

		var infos = await getGameProviderQueryService.GetAsync(query).ConfigureAwait(false);

		foreach (var info in infos)
		{
			var lang = info.Localization.TryGetValue(cultureCode, out var value)
				? value
				: JsonDocument.Parse("{}");

			yield return new GameProviderModel
			{
				Code = info.Code,
				Description = GetLocalization(
					lang.RootElement,
					"description",
					info.Description),
				IsTransferWallet = info.IsTransferWallet,
				Name = GetLocalization(
					lang.RootElement,
					"name",
					info.Name),
				Status = info.Status,
				GameTypes = Array.Empty<GameType>()
			};
		}
	}

	[HttpGet("maintain", Name = "GetMaintainGameProviders")]
	public IAsyncEnumerable<GameProviderStatusModel> GetMaintainGameProvidersAsync(
		[FromServices] IQueryService<GetNotActiveGameProvidersQuery, GameProviderStatus> queryService)
	{
		var brandId = HttpContext.Items.GetBrandId();

		var query = new GetNotActiveGameProvidersQuery(brandId);

		return queryService.QueryAsync(query, HttpContext.RequestAborted)
			.Select(status => new GameProviderStatusModel
			{
				GpCode = status.GpCode,
				Status = status.Status
			});
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