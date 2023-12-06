using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tgs.Payment.Clients;
using Tgs.Payment.DibaoPay.Client;
using Tgs.Payment.SBPay.Client;
using Tgs.Payment.SkyPay;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/deposit/momo")]
[ApiController]
public class MomoDepositController : ControllerBase
{
	[HttpPost("skypay-{fragment}")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	[EnableUserBlockedCheck]
	public async Task<DepositFormCreatedResponse> SubmitSkyPayDepositRequestAsync(
		[FromServices] IGetService<CreateDepositFormCommand, Guid> createDepositFormService,
		[FromServices] IGetService<GetBrandPaymentQuery, BrandPaymentInfo> getBrandPaymentService,
		[FromServices] SkyPaySerializer serializer,
		[FromServices] IGetService<GetDepositFormQuery, DepositFormInfo> getDepositFormService,
		[FromServices] ICommandService<SubmitDepositFormThirdPartyOrderNoCommand> updateDepositFormService,
		string fragment,
		[FromBody] SubmitDepositRequest request)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.GetBrandId().Value;

		var brandPayment = await getBrandPaymentService
			.GetAsync(new GetBrandPaymentQuery(request.BrandPaymentId), HttpContext.RequestAborted)
			.ConfigureAwait(false);

		var brandPaymentInfo = JsonDocument.Parse(brandPayment.ExtendedData);

		var command = new CreateDepositFormCommand(
			playerTokenData.PlayerCode,
			brandId,
			request.BrandPaymentId,
			request.Amount,
			$"tgs.payment.skypay.{fragment.ToLower()}.deposit",
			request.PromotionId,
			formId => serializer.Serialize(
				new SkyPayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("accessToken").GetString(),
					brandPaymentInfo.RootElement.GetProperty("gateway").GetString(),
					brandPaymentInfo.RootElement.GetProperty("transfer").GetString(),
					brandPaymentInfo.RootElement.GetProperty("exchangeRate").GetDecimal(),
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

		var applyInfo = serializer.Deserialize<SkyPayDepositApplyInfo>(formData.ApplyInfo!);

		var updateThirdPartyOrderNoCommand = new SubmitDepositFormThirdPartyOrderNoCommand(
			createdFormId,
			applyInfo.Response.TradeNo);

		await updateDepositFormService
			.ExecuteAsync(updateThirdPartyOrderNoCommand, HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return new DepositFormCreatedResponse
		{
			FormId = createdFormId,
			PaymentUrl = applyInfo.Response.TradeUrl
		};
	}

	[HttpPost("sbpay")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	[EnableUserBlockedCheck]
	public async Task<DepositFormCreatedResponse> SubmitSBPayDepositRequestAsync(
		[FromServices] IGetService<CreateDepositFormCommand, Guid> createDepositFormService,
		[FromServices] IGetService<GetBrandPaymentQuery, BrandPaymentInfo> getBrandPaymentService,
		[FromServices] IGetService<GetDepositFormQuery, DepositFormInfo> getDepositFormService,
		[FromServices] ICommandService<SubmitDepositFormThirdPartyOrderNoCommand> updateDepositFormService,
		[FromBody] SubmitDepositRequest request)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.GetBrandId().Value;

		var brandPayment = await getBrandPaymentService
			.GetAsync(new GetBrandPaymentQuery(request.BrandPaymentId), HttpContext.RequestAborted)
			.ConfigureAwait(false);

		var brandPaymentInfo = JsonDocument.Parse(brandPayment.ExtendedData);

		var command = new CreateDepositFormCommand(
			playerTokenData.PlayerCode,
			brandId,
			request.BrandPaymentId,
			request.Amount,
			$"tgs.payment.sbpay.deposit",
			request.PromotionId,
			formId => JsonSerializer.Serialize(
				new SBPayDepositSettings(
					ApiToken: brandPaymentInfo.RootElement.GetProperty("apiToken").GetString(),
					NotifyToken: brandPaymentInfo.RootElement.GetProperty("notifyToken").GetString(),
					formId,
					null), SBPayUtility.SerializerOptions));

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

		var applyInfo = JsonSerializer.Deserialize<SBPayDepositApplyInfo>(formData.ApplyInfo!, SBPayUtility.SerializerOptions);

		var updateThirdPartyOrderNoCommand = new SubmitDepositFormThirdPartyOrderNoCommand(
			createdFormId,
			applyInfo.Response.ThirdPartyOrderNo);

		await updateDepositFormService
			.ExecuteAsync(updateThirdPartyOrderNoCommand, HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return new DepositFormCreatedResponse
		{
			FormId = createdFormId,
			PaymentUrl = applyInfo.Response.RedirectUrl.ToString(),
		};
	}

	[HttpPost("dibaopay")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	[EnableUserBlockedCheck]
	public async Task<DepositFormCreatedResponse> SubmitDibaoPayDepositRequestAsync(
		[FromServices] IGetService<CreateDepositFormCommand, Guid> createDepositFormService,
		[FromServices] IGetService<GetBrandPaymentQuery, BrandPaymentInfo> getBrandPaymentService,
		[FromServices] IGetService<GetDepositFormQuery, DepositFormInfo> getDepositFormService,
		[FromServices] ICommandService<SubmitDepositFormThirdPartyOrderNoCommand> updateDepositFormService,
		[FromBody] SubmitDepositRequest request)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.GetBrandId().Value;

		var brandPayment = await getBrandPaymentService
			.GetAsync(new GetBrandPaymentQuery(request.BrandPaymentId), HttpContext.RequestAborted)
			.ConfigureAwait(false);

		var brandPaymentInfo = JsonDocument.Parse(brandPayment.ExtendedData);

		var command = new CreateDepositFormCommand(
			playerTokenData.PlayerCode,
			brandId,
			request.BrandPaymentId,
			request.Amount,
			$"tgs.payment.dibaopay.deposit",
			request.PromotionId,
			formId => JsonSerializer.Serialize(
				new DibaoPayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("merchantId").GetString(),
					brandPaymentInfo.RootElement.GetProperty("privateKey").GetString(),
					formId,
					brandPaymentInfo.RootElement.GetProperty("channel").GetString(),
					HttpContext.Connection.RemoteIpAddress.ToString()),
				DibaoPayUtility.SerializerOptions));

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

		var applyInfo = JsonSerializer.Deserialize<DibaoPayDepositApplyInfo>(formData.ApplyInfo!, DibaoPayUtility.SerializerOptions);

		var updateThirdPartyOrderNoCommand = new SubmitDepositFormThirdPartyOrderNoCommand(
			createdFormId,
			applyInfo.Response.ThirdPartyOrderNo);

		await updateDepositFormService
			.ExecuteAsync(updateThirdPartyOrderNoCommand, HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return new DepositFormCreatedResponse
		{
			FormId = createdFormId,
			PaymentUrl = applyInfo.Response.RedirectUrl.ToString(),
		};
	}
}
