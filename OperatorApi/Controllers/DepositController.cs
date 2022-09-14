using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Tgs.Payment.Clients;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Core.Infrastructures.Extensions;
using TGS.Core.Models.Messages;
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

        var query = new DepositFormsQuery(
            brandId,
            playerTokenData.PlayerCode,
            request.Status.ToPaymentFlowState(),
            createOnRange,
            updateOnRange,
            paging);

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
    [AuthorizePlayerToken(AllowNullOrEmpty = true, NeedAuthorize = true)]
    public Task<Dictionary<int, string>> GetDepositFormStatusAsync()
    {
        var dict = Enum.GetValues(typeof(FlowState))
           .Cast<FlowState>()
           .Where(x => x is not (FlowState.Unknown or FlowState.WaitingPaid or FlowState.Paid))
           .ToDictionary(t => (int)t, t => t.ToString());
        return Task.FromResult(dict);
    }

    private static decimal GetHandlingFeeRatePercentile(BrandPaymentFeeInfo[] brandPaymentFeeSettings, Guid brandPaymentId)
    {
        var feeSetting = brandPaymentFeeSettings
            .Where(bpfs => bpfs.BrandPaymentId == brandPaymentId)
            .FirstOrDefault();

        return feeSetting == null ? 0M : feeSetting.HandlingFeeRateTenPercentile * 0.1M;
    }
}
