using System.ComponentModel;
using System.Net.Mime;
using System.Text.Json;
using Google.Protobuf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tgs.MessageQueue;
using Tgs.Promotion.Clients;
using Tgs.Promotion.Clients.Protos;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
public class PromotionController : ControllerBase
{
	private readonly IPromotionClient _promotionClient;

	public PromotionController(IPromotionClient promotionClient)
	{
		_promotionClient = promotionClient ?? throw new ArgumentNullException(nameof(promotionClient));
	}

	//[HttpGet("{language}", Name = "GetPromotions")]
	//[Description("Get promotions")]
	//[Consumes(MediaTypeNames.Application.Json)]
	//[AuthorizePlayerToken(AllowNullOrEmpty = true, NeedAuthorize = false)]
	//[Obsolete("預計淘汰")]
	//public async ValueTask<PagingResponse<PromotionResponse>> GetPromotionsAsync(string language,
	//	[FromQuery] PagingRequest request)
	//{
	//	var brandId = HttpContext.Items.GetBrandId();

	//	var promotionsRequest = new GetValidPromotionsRequest
	//	{
	//		BrandId = brandId,
	//		Language = language,
	//		Page = request.Page,
	//		Rows = request.Rows,
	//		OrderBy = request.IsDesc ? (short)SortOrder.Descending : (short)SortOrder.Ascending,
	//		SortBy = "Number",
	//		ShowAll = request.ShowAll
	//	};
	//	var response = await _promotionClient.GetValidPromotionsAsync(promotionsRequest);

	//	return new PagingResponse<PromotionResponse>
	//	{
	//		TotalCount = response.TotalCount,
	//		Items = response.Items.Select(item => new PromotionResponse
	//		{
	//			AmountUsed = item.AmountUsed,
	//			AutoSentPlayerBonus = item.AutoSentPlayerBonus,
	//			BonusAmountLimit = item.BonusAmountLimit,
	//			BrandId = item.BrandId,
	//			Content = item.Content,
	//			CreatedByUsername = item.CreatedByUsername,
	//			CreatedOn = item.CreatedOn,
	//			DesktopBanner = item.DesktopBanner,
	//			From = item.From,
	//			GameProviderIds = item.GameProviderIds,
	//			GameType = (GameType?)item.GameType,
	//			Id = item.Id,
	//			Localization = item.Localization?.ContainsKey(language) == true
	//				? item.Localization[language]
	//				: JsonDocument.Parse("{}"),
	//			MobileBanner = item.MobileBanner,
	//			Name = item.Name,
	//			Percentage = item.Percentage,
	//			PromotionType = item.PromotionType,
	//			Status = (SwitchStatus)item.Status,
	//			SubSubject = item.SubSubject,
	//			Terms = item.Terms,
	//			To = item.To,
	//			TotalAmountLimit = item.TotalAmountLimit,
	//			TurnoverMultiple = item.TurnoverMultiple,
	//			Unlimited = item.Unlimited,
	//			UpdatedByUsername = item.UpdatedByUsername,
	//			UpdatedOn = item.UpdatedOn,
	//			VipLevels = item.VipLevels
	//		})
	//	};
	//}

	[HttpPost("{promotionId}/join", Name = "JoinPromotion")]
	[Description("Join a promotion")]
	[Consumes(MediaTypeNames.Application.Json)]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async ValueTask<JoinPromotionResponse> JoinPromotionAsync(Guid promotionId)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var response = await _promotionClient.JoinAsync(new(promotionId, playerTokenData.PlayerCode));

		return new() { Success = response.Valid, ErrorMessage = !response.Valid ? response.Message : string.Empty };
	}

	[HttpGet(Name = "ListPromotions")]
	[AuthorizePlayerToken(AllowNullOrEmpty = true, NeedAuthorize = false)]
	public IAsyncEnumerable<PromotionViewModel> ListPromotionsAsync(
		[FromServices] IPromotionServiceClient promotionServiceClient,
		[FromServices] IOptions<JsonOptions> jsonOptionsAccessor)
	{
		var brandId = HttpContext.Items.GetBrandId();
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		return (playerTokenData is null
			? promotionServiceClient
				.ListPromotionsAsync(brandId, HttpContext.RequestAborted)
			: promotionServiceClient
				.ListPromotionsAsync(brandId, playerTokenData.PlayerCode, HttpContext.RequestAborted))
				.Select(promotion => new PromotionViewModel
				{
					Id = promotion.Id,
					Name = promotion.Name,
					From = promotion.EventDuring.From,
					To = promotion.EventDuring.To,
					PromotionType = promotion.PromotionType,
					Content = promotion.Content,
					DesktopBanner = promotion.PersonalComputerBanner,
					MobileBanner = promotion.MobileBanner,
					Localization = promotion.Localization,
					SubSubject = promotion.SubSubject,
					Terms = promotion.Convention,
					VipLevels = promotion.VipLevels,
					CampaignDetail = promotion.PromotionType == "Roulette"
						? JsonSerializer.SerializeToDocument(
							promotion.CampaignDetail.Deserialize<RouletteSettingsViewModel>(jsonOptionsAccessor.Value.JsonSerializerOptions)!,
							jsonOptionsAccessor.Value.JsonSerializerOptions)
						: promotion.CampaignDetail,
					Order = promotion.Order
				});
	}

	[HttpGet("roulette/count", Name = "ListRouletteCountItems")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async IAsyncEnumerable<RouletteCountItemViewModel> ListRouletteCountItemsAsync(
		[FromServices] IMessageSender messageSender)
	{
		var promotionTypes = new[] { "DepositAccumulateRoulette" };

		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var rouletteCountItems = new List<RouletteCountItemViewModel>();

		var tasks = promotionTypes.Select(promotionType =>
		{
			var request = new RouletteCountRequest { PlayerCode = playerTokenData.PlayerCode };

			var subject = $"tgs.promotion.{promotionType.ToLower()}.roulettecountitems.query";

			var task = Task.Run(async () =>
			{
				var data = await messageSender
					.RequestAsync(subject, request.ToByteArray())
					.ConfigureAwait(false);

				var response = RouletteCountResponse.Parser.ParseFrom(data.Span);

				var items = response.Items
					.Select(item => new RouletteCountItemViewModel
					{
						PromotionId = item.PromotionId,
						PromotionType = promotionType,
						RouletteCount = item.RouletteCount
					}).ToArray();

				rouletteCountItems.AddRange(items);
			});

			return task;
		}).ToArray();

		await Task.WhenAll(tasks).ConfigureAwait(false);

		foreach (var item in rouletteCountItems)
			yield return item;
	}
}
