using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tgs.Payment.AlfaPay;
using Tgs.Payment.Clients;
using Tgs.Payment.MalakiPay;
using Tgs.Payment.PhpGoPay.Client;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/deposit/thirdparty")]
[ApiController]
public class ThirdPartyDepositController : ControllerBase
{
	[HttpPost("alfapay", Name = "SubmitAlfaPayDepositRequest")]
	[EnableUserBlockedCheck]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<DepositFormCreatedResponse> SubmitAlfaPayDepositRequestAsync(
		[FromServices] IGetService<CreateDepositFormCommand, Guid> createDepositFormService,
		[FromServices] IGetService<GetBrandPaymentQuery, BrandPaymentInfo> getBrandPaymentService,
		[FromServices] AlfaPaySerializer serializer,
		[FromServices] IGetService<GetDepositFormQuery, DepositFormInfo> getDepositFormService,
		[FromServices] ICommandService<SubmitDepositFormThirdPartyOrderNoCommand> updateDepositFormService,
		[FromBody] SubmitDepositRequest request)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.Items.GetBrandId();

		var brandPayment = await getBrandPaymentService
			.GetAsync(new GetBrandPaymentQuery(request.BrandPaymentId), HttpContext.RequestAborted)
			.ConfigureAwait(false);

		var brandPaymentInfo = JsonDocument.Parse(brandPayment.ExtendedData);

		var command = new CreateDepositFormCommand(
			playerTokenData.PlayerCode,
			brandId,
			request.BrandPaymentId,
			request.Amount,
			"tgs.payment.alfapay.deposit",
			request.PromotionId,
			formId => serializer.Serialize(
				new AlfaPayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("apiToken").GetString(),
					brandPaymentInfo.RootElement.GetProperty("notifyToken").GetString(),
					formId,
					null)));

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

		var applyInfo = serializer.Deserialize<AlfaPayDepositApplyInfo>(formData.ApplyInfo!);

		var updateThirdPartyOrderNoCommand = new SubmitDepositFormThirdPartyOrderNoCommand(
			createdFormId,
			applyInfo.Response.Data.TradeNo);

		await updateDepositFormService
			.ExecuteAsync(updateThirdPartyOrderNoCommand, HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return new DepositFormCreatedResponse
		{
			FormId = createdFormId,
			PaymentUrl = applyInfo.Response.Data.Uri.ToString()
		};
	}

	[HttpPost("malakipay", Name = "SubmitMalakiPayDepositRequest")]
	[HttpPost("~/api/deposit/bank/malakipay", Name = "SubmitMalakiPayBankDepositRequest")]
	[EnableUserBlockedCheck]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<DepositFormCreatedResponse> SubmitMalakiPayDepositRequestAsync(
		[FromServices] IGetService<CreateDepositFormCommand, Guid> createDepositFormService,
		[FromServices] IGetService<GetBrandPaymentQuery, BrandPaymentInfo> getBrandPaymentService,
		[FromServices] MalakiPaySerializer serializer,
		[FromServices] IGetService<GetDepositFormQuery, DepositFormInfo> getDepositFormService,
		[FromServices] ICommandService<SubmitDepositFormThirdPartyOrderNoCommand> updateDepositFormService,
		[FromBody] SubmitDepositRequest request)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.Items.GetBrandId();

		var brandPayment = await getBrandPaymentService
			.GetAsync(new GetBrandPaymentQuery(request.BrandPaymentId), HttpContext.RequestAborted)
			.ConfigureAwait(false);

		var brandPaymentInfo = JsonDocument.Parse(brandPayment.ExtendedData);

		var command = new CreateDepositFormCommand(
			playerTokenData.PlayerCode,
			brandId,
			request.BrandPaymentId,
			request.Amount,
			"tgs.payment.malakipay.deposit",
			request.PromotionId,
			formId => serializer.Serialize(
				new MalakiPayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("merchant").GetString(),
					brandPaymentInfo.RootElement.GetProperty("secret").GetString(),
					brandPaymentInfo.RootElement.GetProperty("payType").GetString(),
					formId)));

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

		var applyInfo = serializer.Deserialize<MalakiPayDepositApplyInfo>(formData.ApplyInfo!);

		var updateThirdPartyOrderNoCommand = new SubmitDepositFormThirdPartyOrderNoCommand(
			createdFormId,
			applyInfo.Response.TradeNo);

		await updateDepositFormService
			.ExecuteAsync(updateThirdPartyOrderNoCommand, HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return new DepositFormCreatedResponse
		{
			FormId = createdFormId,
			PaymentUrl = applyInfo.Response.RedirectUrl.ToString()
		};
	}

	[HttpPost("phpgopay", Name = "SubmitPhpGoPay3rdDepositRequest")]
	[EnableUserBlockedCheck]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<DepositFormCreatedResponse> SubmitPhpGoPayDepositRequestAsync(
		[FromServices] IGetService<CreateDepositFormCommand, Guid> createDepositFormService,
		[FromServices] IGetService<GetBrandPaymentQuery, BrandPaymentInfo> getBrandPaymentService,
		[FromServices] IGetService<GetDepositFormQuery, DepositFormInfo> getDepositFormService,
		[FromServices] ICommandService<SubmitDepositFormThirdPartyOrderNoCommand> updateDepositFormService,
		[FromServices] IPlayerServiceClient playerServiceClient,
		[FromBody] SubmitDepositRequest request)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.Items.GetBrandId();

		var brandPayment = await getBrandPaymentService
			.GetAsync(new GetBrandPaymentQuery(request.BrandPaymentId))
			.ConfigureAwait(false);

		var brandPaymentInfo = JsonDocument.Parse(brandPayment.ExtendedData);

		var player = await playerServiceClient
			.GetPlayerInfoAsync(playerTokenData.PlayerCode, HttpContext.RequestAborted)
			.ConfigureAwait(false);

		var playerProperties = await playerServiceClient
			.GetPlayerPropertiesAsync(playerTokenData.PlayerCode, HttpContext.RequestAborted)
			.ConfigureAwait(false);

		var command = new CreateDepositFormCommand(
			playerTokenData.PlayerCode,
			brandId,
			request.BrandPaymentId,
			request.Amount,
			"tgs.payment.phpgopay.deposit",
			request.PromotionId,
			formId => JsonSerializer.Serialize(
				new PhpGoPayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("merchant").GetString(),
					brandPaymentInfo.RootElement.GetProperty("key").GetString(),
					PhpGoPaymentType.Parse(
						brandPaymentInfo.RootElement.GetProperty("paymentType").GetString()),
					PhpGoBankCode.Parse(
						brandPaymentInfo.RootElement.GetProperty("bankCode").GetString()),
					new Uri("http://localhost"),
					formId)));

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

		var applyInfo = JsonSerializer.Deserialize<PhpGoPayDepositApplyInfo>(formData.ApplyInfo!, PhpGoPayUtility.SerializerOptions);

		var updateThirdPartyOrderNoCommand = new SubmitDepositFormThirdPartyOrderNoCommand(
			createdFormId,
			applyInfo.Response.FormId.ToString());

		await updateDepositFormService
			.ExecuteAsync(updateThirdPartyOrderNoCommand, HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return new DepositFormCreatedResponse
		{
			FormId = createdFormId,
			PaymentUrl = applyInfo.Response.RedirectUrl.ToString()
		};
	}
}
