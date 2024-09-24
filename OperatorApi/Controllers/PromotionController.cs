using System.ComponentModel;
using System.Net.Mime;
using System.Text.Json;
using Google.Protobuf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tgs.Promotion.Clients;
using Tgs.Promotion.Clients.Protos;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Apis.OperatorApi.Models.Messages.PromotionMilestone;
using Tgs.Promotion.Milestone.Client;
using Valhalla.MessageQueue;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
public class PromotionController(IPromotionClient promotionClient) : ControllerBase
{
	private readonly IPromotionClient _promotionClient = promotionClient ?? throw new ArgumentNullException(nameof(promotionClient));

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
				.Where(promotion => promotion.PromotionType is not "OfflineDepositReward" or "OfflineLoginReward")
			: promotionServiceClient
				.ListPromotionsAsync(brandId, playerTokenData.PlayerCode, HttpContext.RequestAborted))
				.Where(promotion => promotion.PromotionType is not "OfflineDepositReward" or "OfflineLoginReward")
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
					.RequestAsync<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>(subject, request.ToByteArray())
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

	[HttpGet("milestone-list", Name = "ListMilestone")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async IAsyncEnumerable<PromotionMilestoneModel> GetMilestonesAsync(
		[FromServices] IPromotionServiceClient promotionServiceClient,
		[FromServices] IPromotionMilestoneClient promotionMilestoneClient)
	{
		var brandId = HttpContext.Items.GetBrandId();
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var milestonePromotions = await promotionServiceClient
			.ListPromotionsAsync(brandId, playerTokenData!.PlayerCode, HttpContext.RequestAborted)
			.Where(x => x.PromotionType is "Milestone")
			.ToArrayAsync(HttpContext.RequestAborted);

		var list = new List<PromotionMilestoneModel>();

		foreach (var item in milestonePromotions)
		{
			var playerCondition = await promotionMilestoneClient.GetPlayerConditionAsync(brandId, playerTokenData!.PlayerCode,
				item.EventDuring.From, item.EventDuring.To, HttpContext.RequestAborted);

			var milestoneConditions =
				await promotionMilestoneClient.GetConditionsIncludeCompletedListAsync(item.Id, playerTokenData!.PlayerCode)
					.ToArrayAsync(HttpContext.RequestAborted);

			list.Add(new PromotionMilestoneModel
			{
				Name = item.Name,
				SubSubject = item.SubSubject,
				From = item.EventDuring.From,
				To = item.EventDuring.To,
				TotalDepositAmount = playerCondition.DepositAmount,
				TotalValidBetAmount = playerCondition.ValidBetAmount,
				Conditions = milestoneConditions.Select(s => new MilestoneCondition
				{
					Level = s.Level,
					Bonus = s.Bonus,
					DepositAmount = s.MinimumDeposit,
					ValidBetAmount = s.MinimumValidBet,
					IsSent = s.IsSent
				})
			});
		}

		foreach (var promotionMilestoneModel in list)
		{
			yield return promotionMilestoneModel;
		}
	}
}