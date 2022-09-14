#nullable enable

using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models;
using TGS.Apis.OperatorApi.Models.Messages;
using Tgs.Promotion.Clients;
using TGS.Services.Operator.ApplicationServices.Promotions;
using RouletteResult = Tgs.DepositAccumulateRoulette.Clients.RouletteResult;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/dar")]
[ApiController]
public class DepositAccumulateRouletteController : ControllerBase
{
    [HttpGet("setting/{promotionId}", Name = "GetDepositAccumulateRouletteSetting")]
    [AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
    public async ValueTask<DepositAccumulateRouletteSettingViewModel> GetDepositAccumulateRouletteSettingAsync(
        [FromServices] IPromotionServiceClient promotionServiceClient,
        Guid promotionId)
    {
        var rule = await promotionServiceClient
            .GetPromotionRuleAsync<PromotionRule<RouletteSetting>>(promotionId, HttpContext.RequestAborted)
            .ConfigureAwait(false);

        return new DepositAccumulateRouletteSettingViewModel
        {
            RouletteItems = rule?.CampaignDetail?
                .RouletteItems
                .Select(item => new DepositAccumulateRouletteItemViewModel { Title = item.Title })
                            ?? Array.Empty<DepositAccumulateRouletteItemViewModel>()
        };
    }

    [HttpPost("{promotionId}", Name = "PlayDepositAccumulateRoulette")]
    [AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
    public async ValueTask<DepositAccumulateRouletteResultViewModel> PlayDepositAccumulateRouletteAsync(
        [FromServices] IGetService<PlayDepositAccumulateRouletteCommand, RouletteResult> rouletteService,
        Guid promotionId)
    {
        var playerTokenData = HttpContext.Items.GetPlayerTokenData();
        var playerCode = playerTokenData.PlayerCode;

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
