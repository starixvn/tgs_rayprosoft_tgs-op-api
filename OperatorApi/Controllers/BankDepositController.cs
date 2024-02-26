using Microsoft.AspNetCore.Mvc;
using Tgs.Payment.Clients;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models;
using TGS.Apis.OperatorApi.Models.Deposit;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Deposit;

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

	[HttpPost("{paymentCode}", Name = "SubmitBankDepositRequest")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	[EnableUserBlockedCheck]
	public async Task<DepositFormCreatedResponse> SubmitDibaoPayDepositRequestAsync(
		[FromServices] IGetService<CreateDepositFormCommand, Guid> createDepositFormService,
		[FromServices] IGetService<GetBrandPaymentQuery, BrandPaymentInfo> getBrandPaymentService,
		[FromServices] IGetService<GetDepositFormQuery, DepositFormInfo> getDepositFormService,
		[FromServices] ICommandService<SubmitDepositFormThirdPartyOrderNoCommand> updateDepositFormService,
		[FromServices] DepositFormCommandBuilderFactory builderFactory,
		string paymentCode,
		[FromBody] SubmitDepositRequest request)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.GetBrandId().Value;

		var brandPayment = await getBrandPaymentService
			.GetAsync(new GetBrandPaymentQuery(request.BrandPaymentId))
			.ConfigureAwait(false);

		var builder = builderFactory.CreateBuilder(paymentCode);
		var requestInfo = new DepositRequestInfo(
			playerTokenData.PlayerCode,
			brandId,
			brandPayment,
			request.Amount,
			request.PromotionId);

		var command = await builder.BuildDepositCommandAsync(
			paymentCode,
			"bank",
			requestInfo,
			HttpContext.RequestAborted).ConfigureAwait(false);

		var createdFormId = await createDepositFormService
			.GetAsync(command, HttpContext.RequestAborted)
			.ConfigureAwait(false);

		var getDepositFormQuery = new GetDepositFormQuery(
			playerTokenData.PlayerCode,
			createdFormId,
			brandId);

		var formData = await getDepositFormService
			.GetAsync(getDepositFormQuery, HttpContext.RequestAborted)
			.ConfigureAwait(false);

		var applyResponse = await builder.ReadApplyInfoAsync(
			formData.ApplyInfo,
			HttpContext.RequestAborted).ConfigureAwait(false);

		var updateThirdPartyOrderNoCommand = new SubmitDepositFormThirdPartyOrderNoCommand(
			createdFormId,
			applyResponse.ThirdPartyOrderNo);

		await updateDepositFormService
			.ExecuteAsync(updateThirdPartyOrderNoCommand, HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return new DepositFormCreatedResponse
		{
			FormId = createdFormId,
			PaymentUrl = applyResponse.PaymentUrl
		};
	}
}
