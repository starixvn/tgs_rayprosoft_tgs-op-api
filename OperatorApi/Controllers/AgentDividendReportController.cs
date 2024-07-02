using Microsoft.AspNetCore.Mvc;
using Tgs.Agent.Dividend.Client;
using Tgs.BrandCertificate.Clients;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Services.Operator.ApplicationServices.Player;

namespace TGS.Apis.OperatorApi.Controllers;
[Route("api/[controller]")]
[ApiController]
public class AgentDividendReportController : ControllerBase
{
	[HttpGet("beforehand", Name = "GetAgentDividendBeforehand")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<AgentDividendBeforehandViewModel> GetAgentDividendBeforehandAsync(
		[FromServices] IAgentDividendClient agentDividendClient,
		[FromServices] IGetService<GetPlayerAgentQuery, Guid?> playerAgentService,
		[FromServices] IBrandServiceClient brandServiceClient)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.Items.GetBrandId();

		var agentId =
			await playerAgentService.GetAsync(new GetPlayerAgentQuery(playerTokenData!.PlayerCode),
				HttpContext.RequestAborted);

		if (agentId is null)
		{
			return new AgentDividendBeforehandViewModel();
		}

		var response =
			await agentDividendClient.GetAgentDividendBeforehandAsync(brandId, agentId.Value,
				HttpContext.RequestAborted);

		var brandInfo = await brandServiceClient.GetBrandFullInfoAsync(brandId,
			HttpContext.RequestAborted);

		return brandInfo is null
			? new AgentDividendBeforehandViewModel()
			: new AgentDividendBeforehandViewModel
		{
			SettlementIntervalType = brandInfo.SettlementIntervalType,
			Current = response.Current,
			Previous = response.Previous
		};
	}
}
