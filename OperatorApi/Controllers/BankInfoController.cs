using Microsoft.AspNetCore.Mvc;
using Tgs.Payment.Clients;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Services.Operator.ApplicationServices.Withdraw;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BankInfoController : ControllerBase
{
	[HttpPost(Name = "CreatePlayerBankInfo")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task CreateBankInfoAsync(
		[FromServices] ICommandService<CreateBankInfoCommand> createService,
		CreateBankInfoRequest request)
	{
		var brandId = HttpContext.Items.GetBrandId();
		var playerData = HttpContext.Items.GetPlayerTokenData();

		var command = new CreateBankInfoCommand(
			brandId,
			playerData.PlayerCode,
			request.PaymentChannelId,
			request.BankType,
			request.Detail);

		await createService.ExecuteAsync(command).ConfigureAwait(false);
	}

	[HttpDelete("{bankInfoId}", Name = "DeletePlayerBankInfo")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task DeleteBankInfoAsync(
		[FromServices] ICommandService<DeleteBankInfoCommand> deleteService,
		Guid bankInfoId)
	{
		var playerData = HttpContext.Items.GetPlayerTokenData();

		var command = new DeleteBankInfoCommand(
			playerData.PlayerCode,
			bankInfoId);

		await deleteService.ExecuteAsync(command).ConfigureAwait(false);
	}

	[HttpGet(Name = "GetPlayerBankInfos")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public IAsyncEnumerable<BankInfoResponse> GetPlayerBankInfosAsync(
		[FromServices] IQueryService<GetPlayerBankInfosQuery, PlayerBankInfo> queryService)
	{
		var playerData = HttpContext.Items.GetPlayerTokenData();

		var query = new GetPlayerBankInfosQuery(playerData.PlayerCode);

		return queryService.QueryAsync(query)
			.Select(info => new BankInfoResponse
			{
				Id = info.Id,
				PaymentChannelId = info.PaymentChannelId,
				BankType = info.BankType,
				Detail = info.Detail,
				IsDefault = info.IsDefault
			});
	}
}