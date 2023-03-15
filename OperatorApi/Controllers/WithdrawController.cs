using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tgs.Payment.Clients;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Core.Models.Exceptions;
using TGS.Core.Models.Messages;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.BrandPayments;
using TGS.Services.Operator.ApplicationServices.Withdraw;
using TGS.Services.Operator.BrandPayment;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WithdrawController : ControllerBase
{
	[HttpPost]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	[EnableUserBlockedCheck]
	public async Task<Guid> CreateWithdrawRequestAsync(
		[FromServices] IGetService<CheckPlayerIdentityCommand, bool> checkService,
		[FromServices] IGetService<CreateWithdrawFormCommand, bool> checkCanCreateService,
		[FromServices] ICommandService<CreateWithdrawFormCommand> createFormService,
		CreateWithdrawRequest request)
	{
		try
		{
			var brandId = HttpContext.Items.GetBrandId();
			var playerData = HttpContext.Items.GetPlayerTokenData();

			var checkCommand = new CheckPlayerIdentityCommand(brandId, playerData.PlayerCode);

			var playerCanWithdraw = await checkService
				.GetAsync(checkCommand, HttpContext.RequestAborted)
				.ConfigureAwait(false);

			if (playerCanWithdraw is false)
				throw new PlayerCanNotWithdrawException();

			var command = new CreateWithdrawFormCommand(
				playerData.PlayerCode,
				brandId,
				request.BankInfoId,
				request.Amount,
				request.Note);

			if (!await checkCanCreateService
					.GetAsync(command, HttpContext.RequestAborted)
					.ConfigureAwait(false))
				throw new PlayerCanNotWithdrawException();

			await createFormService
				.ExecuteAsync(command, HttpContext.RequestAborted)
				.ConfigureAwait(false);

			return await command.ReturnFormIdSource.Task.ConfigureAwait(false);
		}
		catch (InsufficientFundsException)
		{
			throw new PlayerHasInsufficientFundsException();
		}
	}

	[HttpGet]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	[IgnoreUserBlacklistCheck]
	public async IAsyncEnumerable<BrandPaymentViewModel> GetBrandPaymentsAsync(
		[FromServices] IQueryService<BrandPaymentsQuery, BrandPaymentInfo> brandPaymentsQueryService,
		[FromServices]
			IGetService<AllPaymentMethodsQuery, IDictionary<Guid, PaymentMethodInfo>> getAllPaymentMethodService,
		[FromServices] IGetService<GetPaymentQuery, PaymentInfo> getPaymentService)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.Items.GetBrandId();

		var paymentMethods = await getAllPaymentMethodService
			.GetAsync(new())
			.ConfigureAwait(false);

		var query = new BrandPaymentsQuery(brandId, playerTokenData.PlayerCode);

        var brandPayments = await brandPaymentsQueryService.QueryAsync(query)
            .ToArrayAsync()
            .ConfigureAwait(false);

		foreach (var bp in brandPayments)
		{
			if (bp.TransferType != TransferType.Withdraw)
				continue;

			var getPaymentQuery = new GetPaymentQuery(bp.PaymentId);

			var paymentInfo = await getPaymentService
				.GetAsync(getPaymentQuery)
				.ConfigureAwait(false);

			var paymentMethod = paymentMethods[bp.MethodId];

			yield return new()
			{
				Id = bp.Id,
				ChannelId = bp.ChannelId,
				ChannelName = bp.ChannelName,
				ChannelCode = bp.ChannelCode,
				TransferLimit = new()
				{
					From = bp.TransferLimit.From,
					To = bp.TransferLimit.To
				},
				ExtendedData = JsonDocument.Parse("{}"),
				PaymentId = bp.PaymentId,
				PaymentCode = paymentInfo.Code,
				MethodId = bp.MethodId,
				MethodCode = paymentMethod.Name,
				BankType = paymentMethod.BankType
			};
		}
	}

	/// <summary>
	/// Get the player withdraw info list.
	/// </summary>
	/// <param name="getWithdrawFormsQueryService"></param>
	/// <param name="request"></param>
	/// <param name="paging"></param>
	/// <returns></returns>
	[HttpGet("player/list")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<PagedResponse<WithdrawFormViewModel>> GetWithdrawFormsAsync(
		[FromServices] IGetService<WithdrawFormsQuery, Tgs.Payment.Clients.PagingResponse<WithdrawFormInfo>> getWithdrawFormsQueryService,
		[FromQuery] GetWithdrawFormsRequest request,
		[FromQuery] PageableRequest paging)
	{
		var brandId = HttpContext.Items.GetBrandId();
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var createOnRange = ((DateTimeOffset Begin, DateTimeOffset End)?)null;
		var updateOnRange = ((DateTimeOffset Begin, DateTimeOffset End)?)null;

		if (request.BeginCreateOn != null && request.EndCreateOn != null)
			createOnRange = (request.BeginCreateOn.Value, request.EndCreateOn.Value);

		if (request.BeginUpdateOn != null && request.EndUpdateOn != null)
			updateOnRange = (request.BeginUpdateOn.Value, request.EndUpdateOn.Value);

		var query = new WithdrawFormsQuery(
			brandId,
			playerTokenData.PlayerCode,
			request.Status.ToPaymentFlowState(),
			createOnRange,
			updateOnRange,
			paging);

		var result = await getWithdrawFormsQueryService.GetAsync(query).ConfigureAwait(false);

		var response = new PagedResponse<WithdrawFormViewModel>
		{
			Valid = true,
			TotalCount = result.TotalCount,
			Items = result.Items
				.Select(form => new WithdrawFormViewModel
				{
					Id = form.Id,
					Status = form.Status.ToWithdrawFlowState(),
					Amount = form.ActualAmount ?? form.Amount,
					BrandPaymentId = form.BrandPaymentId,
					CreatedOn = form.CreatedOn,
					UpdatedOn = form.UpdatedOn,
					OrderNo = form.OrderNo
				})
				.ToArray()
		};

		return response;
	}

	[HttpGet("status")]
	[AuthorizePlayerToken(AllowNullOrEmpty = true, NeedAuthorize = false)]
	public Task<Dictionary<int, string>> GetWithdrawFormStatusAsync()
	{
		var dict = Enum.GetValues(typeof(FlowState))
		   .Cast<FlowState>()
		   .Where(x => x is not (FlowState.Unknown or FlowState.Approved))
		   .ToDictionary(t => (int)t, t => t.ToString());
		return Task.FromResult(dict);
	}

	[HttpGet("player/amount")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<decimal> GetWithdrawRequireAmountAsync(
		[FromServices] IGetService<GetRequireBucketAmountQuery, decimal> getService)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var query = new GetRequireBucketAmountQuery(playerTokenData.PlayerCode);
		var wageringRequirement = await getService.GetAsync(query).ConfigureAwait(false);

		return wageringRequirement;
	}
}
