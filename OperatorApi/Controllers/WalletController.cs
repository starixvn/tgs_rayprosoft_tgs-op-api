using Microsoft.AspNetCore.Mvc;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Wallet;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WalletController : ControllerBase
{
	/// <summary>
	/// Gets the balance information.
	/// </summary>
	/// <param name="getBalanceService">The get balance service.</param>
	/// <returns></returns>
	[HttpGet]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<BalanceInfoResponse> GetBalanceAsync(
		[FromServices] IGetService<GetBalanceQuery, BalanceInfo> getBalanceService)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var query = new GetBalanceQuery(playerTokenData.PlayerCode);

		var info = await getBalanceService
			.GetAsync(query)
			.ConfigureAwait(false);

		return new BalanceInfoResponse()
		{
			Main = info.Main
		};
	}
}