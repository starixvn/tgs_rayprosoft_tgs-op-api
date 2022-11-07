using Microsoft.AspNetCore.Mvc;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Promotions;

namespace TGS.Apis.OperatorApi.Controllers;

#nullable enable

[Route("api/sie")]
[ApiController]
public class SignInEventController : ControllerBase
{
	[HttpGet("roulette/{promotionId}", Name = "GetSignInRouletteRecord")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<RouletteResponse?> GetSignInRouletteRecordAsync(
		[FromServices] IGetService<GetSignInRouletteRecordQuery, RouletteResult?> getSignInRouletteRecordService,
		Guid promotionId)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var playerCode = playerTokenData.PlayerCode;

		var query = new GetSignInRouletteRecordQuery(playerCode, promotionId);

		var result = await getSignInRouletteRecordService
			.GetAsync(query, HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return result is null
			? null
			: new RouletteResponse
			{
				Id = result.Value.Id,
				ItemIndex = result.Value.ItemIndex
			};
	}

	[HttpPost("roulette/{promotionId}", Name = "SignInRoulette")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<RouletteResponse> SignInRouletteAsync(
		[FromServices] IGetService<SignInRouletteCommand, RouletteResult> signInService,
		Guid promotionId)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var playerCode = playerTokenData.PlayerCode;

		var command = new SignInRouletteCommand(playerCode, promotionId);

		var result = await signInService
			.GetAsync(command, HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return new RouletteResponse
		{
			Id = result.Id,
			ItemIndex = result.ItemIndex
		};
	}
}
