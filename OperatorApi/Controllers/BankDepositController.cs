using Microsoft.AspNetCore.Mvc;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Models;
using TGS.Services.Operator;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/deposit/bank")]
[ApiController]
public class BankDepositController : ControllerBase
{
	[HttpGet("help2pay/pay/{identityId}")]
	[AuthorizePlayerToken(AllowNullOrEmpty = true, NeedAuthorize = false)]
	public async ValueTask<ContentResult> GetHelp2PayDepositHtmlAsync(
		[FromServices] IHelp2PayRepository redisRepository,
		[FromRoute] Guid identityId)
	{
		var html = await redisRepository.GetDepositHtmlAsync(identityId)
			.ConfigureAwait(false);

		return string.IsNullOrWhiteSpace(html)
			? throw new PaymentFailException($"Can not find help2pay deposit html of identity id:{identityId}")
			: new ContentResult
			{
				StatusCode = StatusCodes.Status200OK,
				ContentType = "text/html",
				Content = html
			};
	}
}