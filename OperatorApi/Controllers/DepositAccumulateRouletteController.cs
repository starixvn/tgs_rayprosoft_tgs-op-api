#nullable enable

using Microsoft.AspNetCore.Mvc;
using Tgs.DepositAccumulateRoulette.Clients;
using Tgs.Promotion.Clients;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Services.Operator.ApplicationServices.Promotions;
using RouletteResult = Tgs.DepositAccumulateRoulette.Clients.RouletteResult;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/dar")]
[ApiController]
public class DepositAccumulateRouletteController : ControllerBase
{
	[HttpGet("roulette/{promotionId}", Name = "GetDepositAccumulateRouletteGameRecords")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async IAsyncEnumerable<DepositAccumulateRouletteGameRecordViewModel> GetDepositAccumulateRouletteGameRecordsAsync(
		[FromServices] IDepositAccumulateRouletteServiceClient rouletteClient,
		Guid promotionId)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var playerCode = playerTokenData!.PlayerCode;

		var rouletteRecords = rouletteClient.GetRouletteRecordsAsync(promotionId, playerCode, HttpContext.RequestAborted);

		await foreach (var record in rouletteRecords.OrderByDescending(r => r.CreatedOn))
		{
			yield return new DepositAccumulateRouletteGameRecordViewModel
			{
				Id = record.RouletteId,
				Date = record.EventDate,
				ItemIndex = record.ItemIndex,
				CreatedOn = record.CreatedOn
			};
		}
	}

	[HttpGet("setting/{promotionId}", Name = "GetDepositAccumulateRouletteSetting")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async ValueTask<DepositAccumulateRouletteSettingViewModel?> GetDepositAccumulateRouletteSettingAsync(
		[FromServices] IPromotionServiceClient promotionServiceClient,
		Guid promotionId)
	{
		var promotionMainInfo = await promotionServiceClient
			.GetPromotionMainInfoAsync(promotionId, HttpContext.RequestAborted)
			.ConfigureAwait(false);

		if (promotionMainInfo is null or not { PromotionType: "DepositAccumulateRoulette" })
			return null;

		var promotionContent = (await promotionServiceClient
			.GetPromotionContentAsync(promotionId, HttpContext.RequestAborted)
			.ConfigureAwait(false))!;

		var promotionRule = (await promotionServiceClient
			.GetPromotionRuleAsync<DepositAccumulateRouletteSettings>(promotionId, HttpContext.RequestAborted)
			.ConfigureAwait(false))!;

		return new DepositAccumulateRouletteSettingViewModel
		{
			Name = promotionMainInfo.Name,
			Period = new ValueRange<DateTime>
			{
				From = promotionMainInfo.EventDuring.From,
				To = promotionMainInfo.EventDuring.To
			},
			Content = promotionContent.Content,
			Terms = promotionContent.Convention,
			Localization = promotionContent.Localization,
			SubSubject = promotionContent.SubSubject,
			RouletteItems = promotionRule.CampaignDetail!.RouletteItems
				.Select(item => new DepositAccumulateRouletteItemViewModel
				{
					Title = item.Title
				}),
			VipLevels = promotionRule.VipLevels
		};
	}

	[HttpPost("{promotionId}", Name = "PlayDepositAccumulateRoulette")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async ValueTask<DepositAccumulateRouletteResultViewModel> PlayDepositAccumulateRouletteAsync(
		[FromServices] IGetService<PlayDepositAccumulateRouletteCommand, RouletteResult> rouletteService,
		Guid promotionId)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var playerCode = playerTokenData!.PlayerCode;

		var command = new PlayDepositAccumulateRouletteCommand(promotionId, playerCode);

		var result = await rouletteService
			.GetAsync(command, HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return new DepositAccumulateRouletteResultViewModel
		{
			ItemIndex = result.ItemIndex
		};
	}
}
