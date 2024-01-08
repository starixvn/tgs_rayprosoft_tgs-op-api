using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tgs.Payment.Clients;
using Tgs.Payment.LeePay;
using Tgs.Payment.UBPay.Client;
using Tgs.Payment.UmPay;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/deposit/crypto")]
[ApiController]
public class CryptoDepositController : ControllerBase
{
	[HttpGet("leepay/exchangerate/{brandPaymentId}")]
	[AuthorizePlayerToken(AllowNullOrEmpty = true, NeedAuthorize = false)]
	public async Task<GetLeePayExchangeRateResponse> GetExchangeRateAsync(
		[FromServices] IGetService<GetBrandPaymentQuery, BrandPaymentInfo> getBrandPaymentService,
		[FromServices] IGetService<GetLeePayExchangeRateQuery, string> getLeePayExchangeRateService,
		[FromServices] LeePaySerializer serializer,
		string brandPaymentId)
	{
		var brandPayment = await getBrandPaymentService
			.GetAsync(new GetBrandPaymentQuery(new Guid(brandPaymentId)))
			.ConfigureAwait(false);

		var brandPaymentInfo = JsonDocument.Parse(brandPayment.ExtendedData);

		var query = new GetLeePayExchangeRateQuery(
			"tgs.payment.leepay.queryexchangerate",
			serializer.Serialize(
				new LeePayQueryExchangeRateSettings(
					brandPaymentInfo.RootElement.GetProperty("systemId").GetString(),
					brandPaymentInfo.RootElement.GetProperty("systemKey").GetString(),
					"CNY",
					"USDT")));

		var response = await getLeePayExchangeRateService
			.GetAsync(query)
			.ConfigureAwait(false);

		var applyInfo = serializer.Deserialize<LeePayQueryExchangeRateApplyInfo>(response);

		return applyInfo.Response.Code == "00"
			? new GetLeePayExchangeRateResponse
			{
				BuyPrice = applyInfo.Response.BuyPrice.Value.ToString("#.#####"),
				SellPrice = applyInfo.Response.SellPrice.Value.ToString("#.#####"),
				UpdateTime = applyInfo.Response.BizTime
			}
			: throw new LeePayApiException();
	}

	[HttpGet("umpay/exchangerate/{brandPaymentId}")]
	[AuthorizePlayerToken(AllowNullOrEmpty = true, NeedAuthorize = false)]
	public async Task<GetUmPayExchangeRateResponse> GetUmPayExchangeRateAsync(
		[FromServices] IGetService<GetUmPayExchangeRateQuery, string> getUmPayExchangeRateService,
		[FromServices] UmPaySerializer serializer)
	{
		var query = new GetUmPayExchangeRateQuery(
			"tgs.payment.umpay.queryexchangerate",
			serializer.Serialize(
				new UmPayQueryExchangeRateSettings()));

		var response = await getUmPayExchangeRateService
			.GetAsync(query)
			.ConfigureAwait(false);

		var applyInfo = serializer.Deserialize<UmPayQueryExchangeRateApplyInfo>(response);

		return applyInfo.Response.Code == "1"
			? new GetUmPayExchangeRateResponse
			{
				Rate = applyInfo.Response.Rate
			}
			: throw new UmPayApiException();
	}

	[HttpPut("leepay")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	[EnableUserBlockedCheck]
	public async ValueTask<LeePayDepositFormCreatedResponse> SubmitDepositRequestAsync(
		[FromServices] IGetService<CreateDepositFormCommand, Guid> createDepositFormService,
		[FromServices] IGetService<GetBrandPaymentQuery, BrandPaymentInfo> getBrandPaymentService,
		[FromServices] IGetService<GetDepositFormQuery, DepositFormInfo> getDepositFormService,
		[FromServices] ICommandService<SubmitDepositFormThirdPartyOrderNoCommand> updateDepositFormService,
		[FromServices] LeePaySerializer serializer,
		[FromBody] SubmitDepositRequest request)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.Items.GetBrandId();

		var brandPayment = await getBrandPaymentService
			.GetAsync(new GetBrandPaymentQuery(request.BrandPaymentId))
			.ConfigureAwait(false);

		var brandPaymentInfo = JsonDocument.Parse(brandPayment.ExtendedData);

		var command = new CreateDepositFormCommand(
			playerTokenData.PlayerCode,
			brandId,
			request.BrandPaymentId,
			request.Amount,
			"tgs.payment.leepay.deposit",
			request.PromotionId,
			formId => serializer.Serialize(
				new LeePayDepositSettings(
						brandPaymentInfo.RootElement.GetProperty("systemId").GetString(),
						brandPaymentInfo.RootElement.GetProperty("systemKey").GetString(),
						formId,
						brandPaymentInfo.RootElement.GetProperty("channelCurrency").GetString(),
						"http://localhost",
						brandPaymentInfo.RootElement.GetProperty("channel").GetString())));

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

		var applyInfo = serializer.Deserialize<LeePayDepositApplyInfo>(formData.ApplyInfo!);

		if (applyInfo.Response.Code != "00")
			throw new PaymentFailException(applyInfo.Response.Message);

		var updateThirdPartyOrderNoCommand = new SubmitDepositFormThirdPartyOrderNoCommand(
			createdFormId,
			applyInfo.Response.OrderNo);

		await updateDepositFormService
			.ExecuteAsync(updateThirdPartyOrderNoCommand, HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return new LeePayDepositFormCreatedResponse
		{
			FormId = createdFormId,
			PaymentUrl = applyInfo.Response.PaymentUrl
		};
	}

	[HttpGet("ubpay/exchangerate/{brandPaymentId}")]
	[AuthorizePlayerToken(AllowNullOrEmpty = true, NeedAuthorize = false)]
	public async Task<GetUBPayExchangeRateResponse> GetUBPayExchangeRateAsync(
		[FromServices] IGetService<GetBrandPaymentQuery, BrandPaymentInfo> getBrandPaymentService,
		[FromServices] IGetService<GetUBPayExchangeRateQuery, string> getUBPayExchangeRateService,
		string brandPaymentId)
	{
		var brandPayment = await getBrandPaymentService
			.GetAsync(new GetBrandPaymentQuery(new Guid(brandPaymentId)))
			.ConfigureAwait(false);

		var brandPaymentInfo = JsonDocument.Parse(brandPayment.ExtendedData);

		var canGetPlatform = brandPaymentInfo.RootElement.TryGetProperty("platform", out var jsonPlatform);

		var query = new GetUBPayExchangeRateQuery(
			"tgs.payment.ubpay.queryexchangerate",
			JsonSerializer.Serialize(
				new UBPayQueryExchangeRateSettings(
					brandPaymentInfo.RootElement.GetProperty("apiToken").GetString(),
					brandPaymentInfo.RootElement.GetProperty("currency").GetString(),
					brandPaymentInfo.RootElement.GetProperty("exchangeRate").GetDecimal(),
					canGetPlatform ? jsonPlatform.GetString() : "binance"),
				UBPayUtility.SerializerOptions));

		var response = await getUBPayExchangeRateService
			.GetAsync(query)
			.ConfigureAwait(false);

		var applyInfo = JsonSerializer.Deserialize<UBPayQueryExchangeRateApplyInfo>(response, UBPayUtility.SerializerOptions);

		return new GetUBPayExchangeRateResponse
		{
			Rate = applyInfo.Response.Rate
		};
	}

	[HttpPost("umpay")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	[EnableUserBlockedCheck]
	public async ValueTask<UmPayDepositFormCreatedResponse> SubmitUmPayDepositRequestAsync(
		[FromServices] IGetService<CreateDepositFormCommand, Guid> createDepositFormService,
		[FromServices] IGetService<GetBrandPaymentQuery, BrandPaymentInfo> getBrandPaymentService,
		[FromServices] IGetService<GetUmPayExchangeRateQuery, string> getUmPayExchangeRateService,
		[FromServices] IGetService<GetDepositFormQuery, DepositFormInfo> getDepositFormService,
		[FromServices] ICommandService<SubmitDepositFormThirdPartyOrderNoCommand> updateDepositFormService,
		[FromServices] UmPaySerializer serializer,
		[FromBody] SubmitUmPayDepositRequest request)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.Items.GetBrandId();

		var brandPayment = await getBrandPaymentService
			.GetAsync(new GetBrandPaymentQuery(request.BrandPaymentId), HttpContext.RequestAborted)
			.ConfigureAwait(false);

		var brandPaymentInfo = JsonDocument.Parse(brandPayment.ExtendedData);

		var query = new GetUmPayExchangeRateQuery(
			"tgs.payment.umpay.queryexchangerate",
			serializer.Serialize(
				new UmPayQueryExchangeRateSettings()));

		var response = await getUmPayExchangeRateService
			.GetAsync(query, HttpContext.RequestAborted)
			.ConfigureAwait(false);

		var exchangeRateApplyInfo = serializer.Deserialize<UmPayQueryExchangeRateApplyInfo>(response);

		var rateResponse = exchangeRateApplyInfo.Response.Code == "1" ? exchangeRateApplyInfo.Response.Rate : throw new UmPayApiException();

		var currency = brandPaymentInfo.RootElement.GetProperty("currency").GetString();
		var usdt = rateResponse.RootElement.GetProperty("USDT");
		var rate = usdt.GetProperty(currency.ToUpper()).GetProperty("rate").GetDecimal();

		var command = new CreateDepositFormCommand(
			playerTokenData.PlayerCode,
			brandId,
			request.BrandPaymentId,
			request.Amount,
			"tgs.payment.umpay.deposit",
			request.PromotionId,
			formId => serializer.Serialize(
				new UmPayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("apiKey").GetString(),
					brandPaymentInfo.RootElement.GetProperty("merchantNo").GetString(),
					brandPaymentInfo.RootElement.GetProperty("tradeType").GetString(),
					"1",
					formId,
					request.Amount,
					Rate: rate,
					formId.ToString())));

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

		var applyInfo = serializer.Deserialize<UmPayDepositApplyInfo>(formData.ApplyInfo!);

		if (applyInfo.Response.Code != "1")
			throw new PaymentFailException(applyInfo.Response.Msg);

		var updateThirdPartyOrderNoCommand = new SubmitDepositFormThirdPartyOrderNoCommand(
				createdFormId,
				applyInfo.Response.TradeNo);

		await updateDepositFormService
			.ExecuteAsync(updateThirdPartyOrderNoCommand, HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return new UmPayDepositFormCreatedResponse
		{
			FormId = createdFormId,
			PaymentUrl = applyInfo.Response.JumpUrl
		};
	}

	[HttpPost("ubpay")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	[EnableUserBlockedCheck]
	public async ValueTask<DepositFormCreatedResponse> SubmitUBPayDepositRequestAsync(
		[FromServices] IGetService<CreateDepositFormCommand, Guid> createDepositFormService,
		[FromServices] IGetService<GetBrandPaymentQuery, BrandPaymentInfo> getBrandPaymentService,
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

		var canGetCurrency = brandPaymentInfo.RootElement.TryGetProperty("currency", out var jsonCurrency);

		var command = new CreateDepositFormCommand(
			playerTokenData.PlayerCode,
			brandId,
			request.BrandPaymentId,
			request.Amount,
			"tgs.payment.ubpay.deposit",
			request.PromotionId,
			formId => JsonSerializer.Serialize(
				new UBPayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("apiToken").GetString(),
					brandPaymentInfo.RootElement.GetProperty("notifyToken").GetString(),
					formId,
					canGetCurrency ? jsonCurrency.GetString() : null,
					brandPaymentInfo.RootElement.GetProperty("exchangeRate").GetDecimal())));

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

		var applyInfo = JsonSerializer.Deserialize<UBPayDepositApplyInfo>(formData.ApplyInfo!, UBPayUtility.SerializerOptions);

		var updateThirdPartyOrderNoCommand = new SubmitDepositFormThirdPartyOrderNoCommand(
			createdFormId,
			applyInfo.Response.ThirdPartyOrderNo);

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
