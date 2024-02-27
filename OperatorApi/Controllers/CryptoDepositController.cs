using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tgs.Payment.Clients;
using Tgs.Payment.LeePay;
using Tgs.Payment.UBPay.Client;
using Tgs.Payment.UmPay;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
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
			.GetAsync(query, HttpContext.RequestAborted)
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
}