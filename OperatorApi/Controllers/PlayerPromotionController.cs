using System.ComponentModel;
using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tgs.Promotion.Clients;
using Tgs.Promotion.Clients.Models.Promotion;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Core.Models.Enums;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
public class PlayerPromotionController(IPromotionClient client) : ControllerBase
{
	/// <summary>
	/// Get promotions that the player has already joined in.
	/// </summary>
	[HttpGet("joined", Name = "GetJoinedPromotions")]
	[Description("Get promotions that the player has already joined in")]
	[Consumes(MediaTypeNames.Application.Json)]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<PagingResponse<JoinedPromotionsResponse>> GetJoinedPromotionsAsync(
		[FromQuery] GetPlayerPromotionRecordRequest request)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var joinedPromotionRequest = new GetJoinedPromotionsRequest
		{
			PlayerCode = playerTokenData.PlayerCode,
			From = request.From,
			To = request.To,
			Page = request.Page,
			Rows = request.Rows,
			OrderBy = (short)(request.IsDesc ? 1 : 0),
			SortBy = request.SortBy,
			ShowAll = request.ShowAll
		};

		var response = await client.GetJoinedPromotionsAsync(joinedPromotionRequest);

		return new PagingResponse<JoinedPromotionsResponse>
		{
			TotalCount = response.TotalCount,
			Items = response.Items.Select(item => new JoinedPromotionsResponse
			{
				PromotionName = item.PromotionName,
				JoinedOn = item.CreatedOn
			})
		};
	}

	[HttpGet("{language}", Name = "GetPlayerPromotions")]
	[Description("Get player's promotions")]
	[Consumes(MediaTypeNames.Application.Json)]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	[Obsolete("預計淘汰")]
	public async ValueTask<PagingResponse<PromotionResponse>> GetPlayerPromotionsAsync(string language,
		[FromQuery] PagingRequest request)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.Items.GetBrandId();

		var promotionsRequest = new GetValidPromotionsRequest
		{
			BrandId = brandId,
			Language = language,
			PlayerCode = playerTokenData.PlayerCode,
			Page = request.Page,
			Rows = request.Rows,
			OrderBy = (short)(request.IsDesc ? 1 : 0),
			SortBy = request.SortBy,
			ShowAll = request.ShowAll
		};

		var response = await client.GetValidPromotionsAsync(promotionsRequest);

		return new PagingResponse<PromotionResponse>
		{
			TotalCount = response.TotalCount,
			Items = response.Items.Select(item => new PromotionResponse
			{
				AmountUsed = item.AmountUsed,
				AutoSentPlayerBonus = item.AutoSentPlayerBonus,
				BonusAmountLimit = item.BonusAmountLimit,
				BrandId = item.BrandId,
				Content = item.Content,
				CreatedByUsername = item.CreatedByUsername,
				CreatedOn = item.CreatedOn,
				DesktopBanner = item.DesktopBanner,
				From = item.From,
				GameProviderIds = item.GameProviderIds,
				GameType = (GameType?)item.GameType,
				Id = item.Id,
				Localization = item.Localization?.ContainsKey(language) == true
					? item.Localization[language]
					: JsonDocument.Parse("{}"),
				MobileBanner = item.MobileBanner,
				Name = item.Name,
				Percentage = item.Percentage,
				PromotionType = item.PromotionType,
				Status = (SwitchStatus)item.Status,
				SubSubject = item.SubSubject,
				Terms = item.Terms,
				To = item.To,
				TotalAmountLimit = item.TotalAmountLimit,
				TurnoverMultiple = item.TurnoverMultiple,
				Unlimited = item.Unlimited,
				UpdatedByUsername = item.UpdatedByUsername,
				UpdatedOn = item.UpdatedOn,
				VipLevels = item.VipLevels
			})
		};
	}
}