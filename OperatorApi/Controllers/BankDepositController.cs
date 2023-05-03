using System.Text.Json;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Tgs.Payment.Clients;
using Tgs.Payment.Help2Pay;
using Tgs.Payment.MaticPay;
using Tgs.Payment.MetaPay.Client;
using Tgs.Payment.PhpGoPay.Client;
using Tgs.Payment.PrimePay;
using Tgs.Payment.SkyPay;
using Tgs.Payment.TuPay;
using Tgs.Payment.UmPay;
using Tgs.Payment.WandaPay.Client;
using Tgs.Payment.ZeroNineNinePay;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Core.Models.Exceptions;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/deposit/bank")]
[ApiController]
public class BankDepositController : ControllerBase
{
	private static readonly Random _Random = new();
	private readonly ILogger<BankDepositController> _loger;

	public BankDepositController(ILogger<BankDepositController> loger)
	{
		_loger = loger ?? throw new ArgumentNullException(nameof(loger));
	}

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

	[HttpPost("help2pay")]
	[EnableUserBlockedCheck]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<DepositFormCreatedResponse> SubmitHelp2PayDepositRequestAsync(
		[FromServices] IGetService<CreateDepositFormCommand, Guid> createDepositFormService,
		[FromServices] IGetService<GetBrandPaymentQuery, BrandPaymentInfo> getBrandPaymentService,
		[FromServices] IGetService<GetDepositFormQuery, DepositFormInfo> getDepositFormService,
		[FromServices] ICommandService<SubmitDepositFormThirdPartyOrderNoCommand> updateDepositFormService,
		[FromServices] IHelp2PayRepository redisRepository,
		[FromServices] Help2PaySerializer serializer,
		[FromBody] SubmitDepositRequest request)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.Items.GetBrandId();

		var brandPayment = await getBrandPaymentService
			.GetAsync(new GetBrandPaymentQuery(request.BrandPaymentId))
			.ConfigureAwait(false);

		var brandPaymentInfo = JsonDocument.Parse(brandPayment.ExtendedData!);

		var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

		var command = new CreateDepositFormCommand(
			playerTokenData.PlayerCode,
			brandId,
			request.BrandPaymentId,
			request.Amount,
			"tgs.payment.help2pay.deposit",
			request.PromotionId,
			formId => serializer.Serialize(
				new Help2PayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("merchantId").GetString(),
					brandPaymentInfo.RootElement.GetProperty("securityCode").GetString(),
					brandPaymentInfo.RootElement.GetProperty("currency").GetString(),
					brandPaymentInfo.RootElement.GetProperty("bankCode").GetString(),
					formId,
					"https://google.com",
					clientIp,
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

		var applyInfo = serializer.Deserialize<Help2PayDepositApplyInfo>(formData.ApplyInfo!);

		if (!string.IsNullOrWhiteSpace(applyInfo.Response.PaymentDepositId))
		{
			var updateThirdPartyOrderNoCommand = new SubmitDepositFormThirdPartyOrderNoCommand(
				createdFormId,
				applyInfo.Response.PaymentDepositId);

			await updateDepositFormService
				.ExecuteAsync(updateThirdPartyOrderNoCommand, HttpContext.RequestAborted)
				.ConfigureAwait(false);

			var identityId = Guid.NewGuid();

			_ = await redisRepository.StoreDepositHtmlAsync(identityId, applyInfo.Response.Html)
				.ConfigureAwait(false);

			return new DepositFormCreatedResponse
			{
				FormId = createdFormId,
				PaymentUrl = $"/api/deposit/bank/help2pay/pay/{identityId}"
			};
		}

		throw new PaymentFailException("Something went wrong to deposit via help2pay");
	}

	[HttpPost("maticpay")]
	[EnableUserBlockedCheck]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<MaticPayDepositFormCreatedResponse> SubmitMaticPayDepositRequestAsync(
	   [FromServices] IGetService<CreateDepositFormCommand, Guid> createDepositFormService,
	   [FromServices] IGetService<GetBrandPaymentQuery, BrandPaymentInfo> getBrandPaymentService,
	   [FromServices] IGetService<GetDepositFormQuery, DepositFormInfo> getDepositFormService,
	   [FromServices] ICommandService<SubmitDepositFormThirdPartyOrderNoCommand> updateDepositFormService,
	   [FromServices] IPlayerServiceClient playerServiceClient,
	   [FromServices] MaticPaySerializer serializer,
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

		if (player is null || !player.IdentityAuthentication)
			throw new MaticPayDepositNeedRealNameException();

		var playerProperties = await playerServiceClient
			.GetPlayerPropertiesAsync(playerTokenData.PlayerCode, HttpContext.RequestAborted)
			.ConfigureAwait(false);

		var realName = playerProperties.GetValueOrDefault("RealName");

		var command = new CreateDepositFormCommand(
			playerTokenData.PlayerCode,
			brandId,
			request.BrandPaymentId,
			request.Amount,
			"tgs.payment.maticpay.deposit",
			request.PromotionId,
			formId => serializer.Serialize(
				new MaticPayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("merchantNo").GetString(),
					brandPaymentInfo.RootElement.GetProperty("apiKey").GetString(),
					formId,
					request.Amount,
					realName)));

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

		var applyInfo = serializer.Deserialize<MaticPayDepositApplyInfo>(formData.ApplyInfo!);

		if (applyInfo.Response.Code == "0")
		{
			var paymentUrl = applyInfo.Response.Detail.PayURL.ToString();

			var payUri = new Uri(paymentUrl);
			var thirdPartyOrderNo = HttpUtility.ParseQueryString(payUri.Query).Get("oid");

			var updateThirdPartyOrderNoCommand = new SubmitDepositFormThirdPartyOrderNoCommand(
				createdFormId,
				thirdPartyOrderNo);

			await updateDepositFormService
				.ExecuteAsync(updateThirdPartyOrderNoCommand, HttpContext.RequestAborted)
				.ConfigureAwait(false);

			if (applyInfo.Response.Detail.PayURL.ToString() == string.Empty)
			{
				paymentUrl = applyInfo.Response.Detail.PayHtml.ToString();
			}

			return new MaticPayDepositFormCreatedResponse
			{
				FormId = createdFormId,
				PaymentUrl = paymentUrl,
				ErrorMessage = string.Empty
			};
		}
		else
		{
			throw new PaymentFailException(applyInfo.Response.Msg);
		}
	}

	[HttpPost("primepay")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	[EnableUserBlockedCheck]
	public async Task<DepositFormCreatedResponse> SubmitPrimePayDepositRequestAsync(
		[FromServices] IGetService<CreateDepositFormCommand, Guid> createDepositFormService,
		[FromServices] IGetService<GetBrandPaymentQuery, BrandPaymentInfo> getBrandPaymentService,
		[FromServices] IGetService<GetDepositFormQuery, DepositFormInfo> getDepositFormService,
		[FromServices] ICommandService<SubmitDepositFormThirdPartyOrderNoCommand> updateDepositFormService,
		[FromServices] PrimePaySerializer serializer,
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
			"tgs.payment.primepay.deposit",
			request.PromotionId,
			formId => serializer.Serialize(
				new PrimePayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("merchantId").GetString(),
					brandPaymentInfo.RootElement.GetProperty("key").GetString(),
					brandPaymentInfo.RootElement.GetProperty("iv").GetString(),
					brandPaymentInfo.RootElement.GetProperty("currency").GetString(),
					formId,
					"https://google.com",
					brandPaymentInfo.RootElement.TryGetProperty("paymentMethod", out var prop)
						? prop.GetString()
						: null)));

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

		var applyInfo = serializer.Deserialize<PrimePayDepositApplyInfo>(formData.ApplyInfo!);

		if (applyInfo.Response is PrimePayDepositSuccessResponse response)
		{
			var updateThirdPartyOrderNoCommand = new SubmitDepositFormThirdPartyOrderNoCommand(
				createdFormId,
				((PrimePayDepositSuccessResponse)applyInfo.Response).ReferenceId);

			await updateDepositFormService
				.ExecuteAsync(updateThirdPartyOrderNoCommand, HttpContext.RequestAborted)
				.ConfigureAwait(false);

			return new DepositFormCreatedResponse
			{
				FormId = createdFormId,
				PaymentUrl = ((PrimePayDepositSuccessResponse)applyInfo.Response).RedirectUrl.ToString()
			};
		}
		else
		{
			var failResponse = (PrimePayDepositFailResponse)applyInfo.Response;

			throw new PaymentFailException(failResponse.Errors);
		}
	}

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
			.GetAsync(new GetBrandPaymentQuery(request.BrandPaymentId))
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

	[HttpPost("tupay", Name = "SubmitTuPayDepositRequest")]
	[EnableUserBlockedCheck]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<DepositFormCreatedResponse> SubmitTuPayDepositRequestAsync(
		[FromServices] IGetService<CreateDepositFormCommand, Guid> createDepositFormService,
		[FromServices] IGetService<GetBrandPaymentQuery, BrandPaymentInfo> getBrandPaymentService,
		[FromServices] TuPaySerializer serializer,
		[FromServices] IGetService<GetDepositFormQuery, DepositFormInfo> getDepositFormService,
		[FromServices] ICommandService<SubmitDepositFormThirdPartyOrderNoCommand> updateDepositFormService,
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
			"tgs.payment.tupay.deposit",
			request.PromotionId,
			formId => serializer.Serialize(
				new TuPayDepositSettings(
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
			.GetAsync(getDepositFormQuery)
			.ConfigureAwait(false);

		var applyInfo = serializer.Deserialize<TuPayDepositApplyInfo>(formData.ApplyInfo!);

		var updateThirdPartyOrderNoCommand = new SubmitDepositFormThirdPartyOrderNoCommand(
			createdFormId,
			applyInfo.Response.TradeNo);

		await updateDepositFormService
			.ExecuteAsync(updateThirdPartyOrderNoCommand)
			.ConfigureAwait(false);

		return new DepositFormCreatedResponse
		{
			FormId = createdFormId,
			PaymentUrl = applyInfo.Response.RedirectUrl.ToString()
		};
	}

	[HttpPost("umpay")]
	[EnableUserBlockedCheck]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<UmPayDepositFormCreatedResponse> SubmitUmPayDepositRequestAsync(
		[FromServices] IGetService<CreateDepositFormCommand, Guid> createDepositFormService,
		[FromServices] IGetService<GetBrandPaymentQuery, BrandPaymentInfo> getBrandPaymentService,
		[FromServices] IGetService<GetDepositFormQuery, DepositFormInfo> getDepositFormService,
		[FromServices] ICommandService<SubmitDepositFormThirdPartyOrderNoCommand> updateDepositFormService,
		[FromServices] UmPaySerializer serializer,
		[FromBody] SubmitDepositRequest request
		)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.Items.GetBrandId();

		var brandPayment = await getBrandPaymentService
			.GetAsync(new GetBrandPaymentQuery(request.BrandPaymentId), HttpContext.RequestAborted)
			.ConfigureAwait(false);

		var brandPaymentInfo = JsonDocument.Parse(brandPayment.ExtendedData);

		//需要隨機兩位小數的通道類型:PAY01支轉支 PAY03轉數快 PAY04網銀
		var randomPointTradeTypes = new string[] { "PAY01", "PAY03", "PAY04" };

		var tradeType = brandPaymentInfo.RootElement.GetProperty("tradeType").GetString();
		var amount = request.Amount;
		if (randomPointTradeTypes.Contains(tradeType))
		{
			amount += _Random.Next(1, 100) / 100M;
		}

		var command = new CreateDepositFormCommand(
			playerTokenData.PlayerCode,
			brandId,
			request.BrandPaymentId,
			amount,
			"tgs.payment.umpay.deposit",
			request.PromotionId,
			formId => serializer.Serialize(
				new UmPayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("apiKey").GetString(),
					brandPaymentInfo.RootElement.GetProperty("merchantNo").GetString(),
					tradeType,
					"1",
					formId,
					amount,
					1m,
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

		if (applyInfo.Response.Code == "1")
		{
			var updateThirdPartyOrderNoCommand = new SubmitDepositFormThirdPartyOrderNoCommand(
				createdFormId,
				applyInfo.Response.TradeNo);

			await updateDepositFormService
				.ExecuteAsync(updateThirdPartyOrderNoCommand, HttpContext.RequestAborted)
				.ConfigureAwait(false);

			return new UmPayDepositFormCreatedResponse
			{
				FormId = createdFormId,
				PaymentUrl = applyInfo.Response.JumpUrl.ToString(),
				ErrorMessage = string.Empty
			};
		}
		else
		{
			throw new PaymentFailException(applyInfo.Response.Msg);
		}
	}

	[HttpPost("wandapay", Name = "SubmitWandaPayDepositRequest")]
	[EnableUserBlockedCheck]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<DepositFormCreatedResponse> SubmitWandaPayDepositRequestAsync(
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

		var realName = playerProperties.GetValueOrDefault("RealName");

		if (string.IsNullOrWhiteSpace(realName))
			throw new WandaPayNeedChineseRealNameException();

		if (IsChinese(realName) == false)
			throw new WandaPayNeedChineseRealNameException();

		var command = new CreateDepositFormCommand(
			playerTokenData.PlayerCode,
			brandId,
			request.BrandPaymentId,
			request.Amount,
			"tgs.payment.wandapay.deposit",
			request.PromotionId,
			formId => JsonSerializer.Serialize(
				new WandaPayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("storeCode").GetString(),
					brandPaymentInfo.RootElement.GetProperty("signKey").GetString(),
					brandPaymentInfo.RootElement.GetProperty("payType").GetString(),
					formId,
					realName)));

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

		var applyInfo = JsonSerializer.Deserialize<WandaPayDepositApplyInfo>(formData.ApplyInfo!);

		var updateThirdPartyOrderNoCommand = new SubmitDepositFormThirdPartyOrderNoCommand(
			createdFormId,
			applyInfo.Response.OrderNo);

		await updateDepositFormService
			.ExecuteAsync(updateThirdPartyOrderNoCommand, HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return new DepositFormCreatedResponse
		{
			FormId = createdFormId,
			PaymentUrl = applyInfo.Response.PayUrl
		};
	}

	[HttpPost("zeronineninepay", Name = "SubmitZeroNineNinePayDepositRequest")]
	[HttpPost("~/api/deposit/thirdparty/zeronineninepay", Name = "SubmitZeroNineNinePayThirdPartyDepositRequest")]
	[EnableUserBlockedCheck]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<DepositFormCreatedResponse> SubmitZeroNineNinePayDepositRequestAsync(
		[FromServices] IGetService<CreateDepositFormCommand, Guid> createDepositFormService,
		[FromServices] IGetService<GetBrandPaymentQuery, BrandPaymentInfo> getBrandPaymentService,
		[FromServices] IGetService<GetDepositFormQuery, DepositFormInfo> getDepositFormService,
		[FromServices] ICommandService<SubmitDepositFormThirdPartyOrderNoCommand> updateDepositFormService,
		[FromServices] ZeroNineNinePaySerializer serializer,
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
			"tgs.payment.zeronineninepay.deposit",
			request.PromotionId,
			formId => serializer.Serialize(
				new ZeroNineNinePayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("merchantId").GetInt32(),
					brandPaymentInfo.RootElement.GetProperty("merchantKey").GetString(),
					brandPaymentInfo.RootElement.GetProperty("payType").GetString(),
					formId,
					"127.0.0.1",
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

		_loger.LogTrace("zeronineninepay formData.ApplyInfo:{formData.ApplyInfo}", formData.ApplyInfo);

		var applyInfo = serializer.Deserialize<ZeroNineNinePayDepositApplyInfo>(formData.ApplyInfo!);

		if (applyInfo.Response.Code == 200)
		{
			var updateThirdPartyOrderNoCommand = new SubmitDepositFormThirdPartyOrderNoCommand(
				createdFormId,
				applyInfo.Response.Data.TradeNo);

			await updateDepositFormService
				.ExecuteAsync(updateThirdPartyOrderNoCommand, HttpContext.RequestAborted)
				.ConfigureAwait(false);

			return new DepositFormCreatedResponse
			{
				FormId = createdFormId,
				PaymentUrl = applyInfo.Response.Data.PayUrl
			};
		}
		else
		{
			throw new PaymentFailException(applyInfo.Response.Message);
		}
	}

	[HttpPost("metapay", Name = "SubmitMetaPayBankDepositRequest")]
	[EnableUserBlockedCheck]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<DepositFormCreatedResponse> SubmitMetaPayDepositRequestAsync(
		[FromServices] IGetService<CreateDepositFormCommand, Guid> createDepositFormService,
		[FromServices] IGetService<GetBrandPaymentQuery, BrandPaymentInfo> getBrandPaymentService,		
		[FromServices] IGetService<GetDepositFormQuery, DepositFormInfo> getDepositFormService,
		[FromServices] ICommandService<SubmitDepositFormThirdPartyOrderNoCommand> updateDepositFormService,
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
			"tgs.payment.metapay.deposit",
			request.PromotionId,
			formId => JsonSerializer.Serialize(
				new MetaPayDepositSettings(
					brandPaymentInfo.RootElement.GetProperty("appId").GetString(),
					brandPaymentInfo.RootElement.GetProperty("privateKey").GetString(),
					brandPaymentInfo.RootElement.GetProperty("publicKey").GetString(),
					MetaPayDepositType.Parse(brandPaymentInfo.RootElement.GetProperty("payType").GetString()),
					new Uri("http://localhost"),
					formId,
					Phone: "",
					RealName: ""),
				MetaPayUtility.SerializerOptions));

		var createdFormId = await createDepositFormService
			.GetAsync(command, HttpContext.RequestAborted)
			.ConfigureAwait(false);

		var getDepositFormQuery = new GetDepositFormQuery(
			playerTokenData.PlayerCode,
			createdFormId,
			brandId);

		var formData = await getDepositFormService
			.GetAsync(getDepositFormQuery)
			.ConfigureAwait(false);

		var applyInfo = JsonSerializer.Deserialize<MetaPayDepositApplyInfo>(
			formData.ApplyInfo!,
			MetaPayUtility.SerializerOptions);

		var updateThirdPartyOrderNoCommand = new SubmitDepositFormThirdPartyOrderNoCommand(
			createdFormId,
			applyInfo.Response.FormId.ToString());

		await updateDepositFormService
			.ExecuteAsync(updateThirdPartyOrderNoCommand)
			.ConfigureAwait(false);

		return new DepositFormCreatedResponse
		{
			FormId = createdFormId,
			PaymentUrl = applyInfo.Response.RedirectUrl.ToString()
		};
	}
	private static bool IsChinese(string strRealName)
	{
		var flag = true;
		int dstringmax = Convert.ToInt32("9fff", 16);
		int dstringmin = Convert.ToInt32("4e00", 16);
		for (var i = 0; i < strRealName.Length; i++)
		{
			var dRange = Convert.ToInt32(Convert.ToChar(strRealName.Substring(i, 1)));
			if (dRange >= dstringmin && dRange < dstringmax)
			{
				flag = true;
			}
			else
			{
				flag = false;
				break;
			}
		}

		return flag;
	}
}
