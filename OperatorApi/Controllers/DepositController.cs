using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tgs.Payment.Clients;
using Tgs.Payment.S88Pay.Client;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Deposit;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Apis.OperatorApi.Utils;
using TGS.Core.Infrastructures.Extensions;
using TGS.Core.Models.Messages;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.BrandPayments;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DepositController : ControllerBase
{
	/// <summary>
	/// Gets the deposit brand payments.
	/// </summary>
	/// <param name="brandPaymentsQueryService">The brand payments query service.</param>
	/// <param name="getAllPaymentMethodService">The get all payment method service.</param>
	/// <param name="getPaymentService">The get payment service.</param>
	/// <param name="paymentServiceClient">The payment service client.</param>
	/// <returns></returns>
	[HttpGet]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async IAsyncEnumerable<BrandPaymentViewModel> GetBrandPaymentsAsync(
		[FromServices] IQueryService<BrandPaymentsQuery, BrandPaymentInfo> brandPaymentsQueryService,
		[FromServices] IGetService<AllPaymentMethodsQuery, IDictionary<Guid, PaymentMethodInfo>> getAllPaymentMethodService,
		[FromServices] IGetService<GetPaymentQuery, PaymentInfo> getPaymentService,
		[FromServices] IPaymentServiceClient paymentServiceClient)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.Items.GetBrandId();

		var paymentMethods = await getAllPaymentMethodService
			.GetAsync(new AllPaymentMethodsQuery())
			.ConfigureAwait(false);

		var query = new BrandPaymentsQuery(brandId, playerTokenData.PlayerCode);

		var brandPayments = await brandPaymentsQueryService.QueryAsync(query)
			.Where(bp => bp.Enabled == true)
			.ToArrayAsync()
			.ConfigureAwait(false);

		var brandPaymentIds = brandPayments.Select(bp => bp.Id).ToArray();

		var brandPaymentFeeSettings = await paymentServiceClient.GetBrandPaymentsFeeSettingsAsync(brandPaymentIds)
			.ToArrayAsync()
			.ConfigureAwait(false);

		var brandPaymentQuery = await brandPayments
			.Where(bp => bp.TransferType == TransferType.Deposit)
			.AsParallel()
			.WithCancellation(HttpContext.RequestAborted)
			.Select(async bp =>
			{
				var getPaymentQuery = new GetPaymentQuery(bp.PaymentId);

				var paymentInfo = await getPaymentService
					.GetAsync(getPaymentQuery)
					.ConfigureAwait(false);

				var paymentMethod = paymentMethods[bp.MethodId];

				return new BrandPaymentViewModel
				{
					Id = bp.Id,
					ChannelId = bp.ChannelId,
					ChannelName = bp.ChannelName,
					ChannelCode = bp.ChannelCode,
					TransferLimit = new Models.ValueRange<decimal>
					{
						From = bp.TransferLimit.From,
						To = bp.TransferLimit.To
					},
					ExtendedData = paymentInfo.Code == "Manual"
						? JsonDocument.Parse(bp.ExtendedData)
						: JsonDocument.Parse("{}"),
					PaymentId = bp.PaymentId,
					PaymentCode = paymentInfo.Code,
					MethodId = bp.MethodId,
					MethodCode = paymentMethod.Name,
					BankType = paymentMethod.BankType,
					DisplayOrder = bp.DisplayOrder,
					HandlingFeeRatePercentile = GetHandlingFeeRatePercentile(brandPaymentFeeSettings, bp.Id),
				};
			})
			.WhenAll()
			.ConfigureAwait(false);

		foreach (var bp in brandPaymentQuery)
		{
			yield return bp;
		}
	}

	/// <summary>
	/// Get the player deposit info list.
	/// </summary>
	/// <param name="getDepositFormsQueryService">The get deposit forms query service.</param>
	/// <param name="brandPaymentsQueryService">The brand payments query service.</param>
	/// <param name="getPaymentService">The get payment service.</param>
	/// <param name="request">The request.</param>
	/// <param name="paging">The paging.</param>
	/// <returns></returns>
	[HttpGet("player/list")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<PagedResponse<DepositFormViewModel>> GetDepositFormsAsync(
		[FromServices] IGetService<DepositFormsQuery, Tgs.Payment.Clients.PagingResponse<DepositFormInfo>> getDepositFormsQueryService,
		[FromServices] IQueryService<BrandPaymentsQuery, BrandPaymentInfo> brandPaymentsQueryService,
		[FromServices] IGetService<GetPaymentQuery, PaymentInfo> getPaymentService,
		[FromQuery] GetDepositFormsRequest request,
		[FromQuery] PagingRequest paging)
	{
		var brandId = HttpContext.Items.GetBrandId();
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var createOnRange = ((DateTimeOffset Begin, DateTimeOffset End)?)null;
		var updateOnRange = ((DateTimeOffset Begin, DateTimeOffset End)?)null;

		if (request.BeginCreateOn != null && request.EndCreateOn != null)
			createOnRange = (request.BeginCreateOn.Value, request.EndCreateOn.Value);

		if (request.BeginUpdateOn != null && request.EndUpdateOn != null)
			updateOnRange = (request.BeginUpdateOn.Value, request.EndUpdateOn.Value);

		var query = new DepositFormsQuery(
			brandId,
			playerTokenData.PlayerCode,
			request.Status.ToPaymentFlowState(),
			createOnRange,
			updateOnRange,
			new Paging
			{
				IsDesc = paging.IsDesc,
				Page = paging.Page,
				Rows = paging.Rows,
				ShowAll = paging.ShowAll,
				SortBy = paging.SortBy
			});

		var result = await getDepositFormsQueryService.GetAsync(query).ConfigureAwait(false);

		var brandPayments = await brandPaymentsQueryService.QueryAsync(
			new BrandPaymentsQuery(brandId, playerTokenData.PlayerCode))
			.ToDictionaryAsync(bp => bp.Id)
			.ConfigureAwait(false);

		var response = new PagedResponse<DepositFormViewModel>
		{
			Valid = true,
			TotalCount = result.TotalCount,
			Items = await result.Items.ToAsyncEnumerable()
				.SelectAwait(async form => new DepositFormViewModel
				{
					Id = form.Id,
					Status = form.Status.ToDepositFlowState(),
					Amount = form.ActualAmount ?? form.Amount,
					BrandPaymentId = form.BrandPaymentId,
					CanUploadCerts = form.Status == PaymentFlowState.WaitPayment
						&& brandPayments.TryGetValue(form.BrandPaymentId, out var info)
						&& (await getPaymentService
							.GetAsync(new GetPaymentQuery(info.PaymentId))
							.ConfigureAwait(false)).Code == "Manual",
					CreatedOn = form.CreatedOn,
					UpdatedOn = form.UpdatedOn,
					OrderNo = form.OrderNo
				})
				.ToArrayAsync()
				.ConfigureAwait(false)
		};
		return response;
	}

	[HttpGet("status")]
	[AuthorizePlayerToken(AllowNullOrEmpty = true, NeedAuthorize = false)]
	public Task<Dictionary<int, string>> GetDepositFormStatusAsync()
	{
		var dict = Enum.GetValues(typeof(FlowState))
		   .Cast<FlowState>()
		   .Where(x => x is not (FlowState.Unknown or FlowState.WaitingPaid or FlowState.Paid))
		   .ToDictionary(t => (int)t, t => t.ToString());
		return Task.FromResult(dict);
	}

	[HttpPost("{bankType}/{paymentCode}", Name = "SubmitDepositRequest")]
	[EnableUserBlockedCheck]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<DepositFormCreatedResponse> SubmitDepositRequestAsync(
		[FromServices] IGetService<CreateDepositFormCommand, Guid> createDepositFormService,
		[FromServices] IGetService<GetBrandPaymentQuery, BrandPaymentInfo> getBrandPaymentService,
		[FromServices] IGetService<GetDepositFormQuery, DepositFormInfo> getDepositFormService,
		[FromServices] ICommandService<SubmitDepositFormThirdPartyOrderNoCommand> updateDepositFormService,
		[FromServices] DepositFormCommandBuilderFactory builderFactory,
		string bankType,
		string paymentCode,
		[FromBody] SubmitDepositRequest request)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.Items.GetBrandId();

		var brandPayment = await getBrandPaymentService
			.GetAsync(new GetBrandPaymentQuery(request.BrandPaymentId))
			.ConfigureAwait(false);

		var builder = builderFactory.CreateBuilder(paymentCode);
		var requestInfo = new DepositRequestInfo(
			playerTokenData.PlayerCode,
			brandId,
			brandPayment,
			request.Amount,
			request.PromotionId,
			IPUtils.GetRequestIP(HttpContext),
			request.CustomData);

		var command = await builder.BuildDepositCommandAsync(
			paymentCode,
			bankType,
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
			PaymentUrl = applyResponse.PaymentUrl,
			ExpirationTime = applyResponse.ExpirationTime
		};
	}

	[HttpGet("s88pay/pay-data/{tokenId}")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async ValueTask<DepositCashRegisterViewModel<S88PayInfoViewModel>> GetS88PayDepositCashRegisterAsync(
		[FromServices] IS88PayRepository repository,
		Guid tokenId)
	{
		var data = await repository
			.GetDepositAsync(tokenId, HttpContext.RequestAborted)
			.ConfigureAwait(false);

		var response = JsonSerializer.Deserialize<S88PayDepositV3Response>(data, S88PayUtility.SerializerOptions);

		return new DepositCashRegisterViewModel<S88PayInfoViewModel>
		{
			Amount = response.Amount,
			Expired = response.ExpiredAt,
			FormId = response.FormId,
			PayInfo = new S88PayInfoViewModel
			{
				Qr = response.RedirectUrl.ToString(),
				Account = response.Number,
				AccountName = response.Name,
				BankName = response.BankName,
				MatchCode = response.MatchCode,
				Type = response.Type.ToString(),
			}
		};
	}

	private static decimal GetHandlingFeeRatePercentile(BrandPaymentFeeInfo[] brandPaymentFeeSettings, Guid brandPaymentId)
	{
		var feeSetting = brandPaymentFeeSettings
			.Where(bpfs => bpfs.BrandPaymentId == brandPaymentId)
			.FirstOrDefault();

		return feeSetting == null ? 0M : feeSetting.HandlingFeeRateTenPercentile * 0.1M;
	}
}