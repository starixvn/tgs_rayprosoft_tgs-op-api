using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tgs.Payment.Clients;
using Tgs.Payment.ManualDeposit;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Payment.ManualDeposit;
using TGS.Services.Operator.ApplicationServices.BrandPayments;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/deposit/manual")]
[ApiController]
public class ManualDepositController : ControllerBase
{
    [HttpGet("{id}")]
    [AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
    public async Task<ManualDepositFormResponse> GetDepositFormAsync(
        [FromServices] IGetService<GetBankDepositFormQuery, DepositFormInfo> getBankDepositService,
        [FromServices]
        IGetService<AllPaymentChannelsQuery, IDictionary<Guid, PaymentChannelInfo>> getAllPaymentChannelService,
        [FromServices]
        IGetService<AllPaymentMethodsQuery, IDictionary<Guid, PaymentMethodInfo>> getAllPaymentMethodService,
        [FromServices] IGetService<GetBrandPaymentQuery, BrandPaymentInfo> getBrandPaymentService,
        [FromServices] ManualDepositSerializer serializer,
        Guid id)
    {
        var playerTokenData = HttpContext.Items.GetPlayerTokenData();
        var brandId = HttpContext.Items.GetBrandId();

        var getBankDepositFormQuery = new GetBankDepositFormQuery(
            playerTokenData.PlayerCode,
            id,
            brandId);

        var paymentChannels = await getAllPaymentChannelService
            .GetAsync(new())
            .ConfigureAwait(false);
        var paymentMethods = await getAllPaymentMethodService
            .GetAsync(new())
            .ConfigureAwait(false);

        var form = await getBankDepositService
            .GetAsync(getBankDepositFormQuery)
            .ConfigureAwait(false);

        var brandPayment = await getBrandPaymentService
            .GetAsync(new(form.BrandPaymentId))
            .ConfigureAwait(false);

        var paymentMethodInfo = paymentMethods[brandPayment.MethodId];

        var bankInfo = GetApplyInfo(serializer, paymentMethodInfo.BankType, form.ApplyInfo);

        if (paymentMethodInfo.BankType == "Bank")
        {
            return new ManualDepositBankFormResponse()
            {
                MethodName = paymentMethods[brandPayment.MethodId].Name,
                ChannelName = paymentChannels[brandPayment.ChannelId].Name,
                Amount = form.Amount,
                BankAccountName = ((ManualDepositBankApplyInfo)bankInfo).ReceiveAccountName,
                BankAccount = ((ManualDepositBankApplyInfo)bankInfo).ReceiveBankAccount,
                BankName = brandPayment.ChannelName
            };
        }
        else if (paymentMethodInfo.BankType == "Fps")
        {
            return new ManualDepositFpsFormResponse()
            {
                MethodName = paymentMethods[brandPayment.MethodId].Name,
                ChannelName = paymentChannels[brandPayment.ChannelId].Name,
                Amount = form.Amount,
                QrCodeFileUrl = ((ManualDepositFpsApplyInfo)bankInfo).QrCodeFileUrl,
                Email = ((ManualDepositFpsApplyInfo)bankInfo).Email,
                FpsId = ((ManualDepositFpsApplyInfo)bankInfo).FpsId,
                Phone = ((ManualDepositFpsApplyInfo)bankInfo).Phone,
                BankName = brandPayment.ChannelName
            };
        }
        else
        {
            return new ManualDepositThirdPartyFormResponse()
            {
                MethodName = paymentMethods[brandPayment.MethodId].Name,
                ChannelName = paymentChannels[brandPayment.ChannelId].Name,
                Amount = form.Amount,
                QrCode = ((ManualDepositThirdPartyApplyInfo)bankInfo).QrCode,
                BankName = brandPayment.ChannelName
            };
        }
    }

    [HttpPut]
    [HttpPut("bank")]
    [EnableUserBlockedCheck]
    [AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
    public async ValueTask<Guid> SubmitDepositBankRequestAsync(
        [FromServices] IGetService<CreateDepositFormCommand, Guid> createDepositFormService,
        [FromServices] IGetService<GetBrandPaymentQuery, BrandPaymentInfo> getBrandPaymentService,
        [FromServices] ManualDepositSerializer serializer,
        [FromBody] SubmitManualDepositBankRequest request)
    {
        var playerTokenData = HttpContext.Items.GetPlayerTokenData();
        var brandId = HttpContext.Items.GetBrandId();

        var brandPayment = await getBrandPaymentService
            .GetAsync(new(request.BrandPaymentId), HttpContext.RequestAborted)
            .ConfigureAwait(false);

        var brandPaymentBankInfo = JsonDocument.Parse(brandPayment.ExtendedData);

        var command = new CreateDepositFormCommand(
            playerTokenData.PlayerCode,
            brandId,
            request.BrandPaymentId,
            request.Amount,
            "tgs.payment.manual.deposit",
            request.PromotionId,
            _ => serializer.Serialize(
                new ManualDepositBankApplyInfo()
                {
                    BankType = request.BankType,
                    SendBankAccount = request.BankAccount,
                    SendBankName = request.BankName,
                    SendNote = request.Note,
                    ReceiveBankAccount = brandPaymentBankInfo.RootElement.GetProperty("bankAccount").GetString(),
                    ReceiveAccountName = brandPaymentBankInfo.RootElement.GetProperty("name").GetString()
                }));

        return await createDepositFormService
            .GetAsync(command, HttpContext.RequestAborted)
            .ConfigureAwait(false);
    }

    [HttpPut("ewallet", Name = "SubmitDepositEWalletRequest")]
    [EnableUserBlockedCheck]
    [AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
    public async ValueTask<Guid> SubmitDepositEWalletRequestAsync(
       [FromServices] IGetService<CreateDepositFormCommand, Guid> createDepositFormService,
       [FromServices] IGetService<GetBrandPaymentQuery, BrandPaymentInfo> getBrandPaymentService,
       [FromServices] ManualDepositSerializer serializer,
       [FromBody] SubmitManualDepositEWalletRequest request)
    {
        var playerTokenData = HttpContext.Items.GetPlayerTokenData();
        var brandId = HttpContext.Items.GetBrandId();

        var brandPayment = await getBrandPaymentService
            .GetAsync(new(request.BrandPaymentId), HttpContext.RequestAborted)
            .ConfigureAwait(false);

        var brandPaymentBankInfo = JsonDocument.Parse(brandPayment.ExtendedData);

        var qrCode = JsonElementExtenstion.GetPropertyValue(brandPaymentBankInfo.RootElement, "qrCode");

        var command = new CreateDepositFormCommand(
            playerTokenData.PlayerCode,
            brandId,
            request.BrandPaymentId,
            request.Amount,
            "tgs.payment.manual.deposit",
            request.PromotionId,
            _ => serializer.Serialize(
                new ManualDepositEWalletApplyInfo
                {
                    EMoneyName = brandPaymentBankInfo.RootElement.GetProperty("eMoneyName").GetString(),
                    Phone = brandPaymentBankInfo.RootElement.GetProperty("phone").GetString(),
                    QrCode = qrCode,
                    SendNote = request.Note,
                    SendEMoneyName = request.EMoneyName,
                    BankType = request.BankType
                }));

        return await createDepositFormService
            .GetAsync(command, HttpContext.RequestAborted)
            .ConfigureAwait(false);
    }

    [HttpPost("{id}")]
    [EnableUserBlockedCheck]
    [AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
    public async ValueTask SubmitDepositFormAsync(
        [FromServices] ICommandService<SubmitBankDepositFormCommand> submitBankDepositFormCommandService,
        [FromServices] ManualDepositSerializer serializer,
        Guid id,
        [FromForm(Name = "file")] IFormFile manualReceipt)
    {
        var playerTokenData = HttpContext.Items.GetPlayerTokenData();
        var brandId = HttpContext.Items.GetBrandId();

        var contentType = manualReceipt.ContentType;
        await using var fileStream = manualReceipt.OpenReadStream();

        var command = new SubmitBankDepositFormCommand(
            brandId,
            playerTokenData.PlayerCode,
            id,
            contentType,
            fileStream,
            id => serializer.Serialize(
                new ManualDepositPaymentReturn(id)));

        await submitBankDepositFormCommandService.ExecuteAsync(command);
    }

    [HttpPut("fps", Name = "SubmitDepositFpsRequest")]
    [EnableUserBlockedCheck]
    [AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
    public async ValueTask<Guid> SubmitDepositFpsRequestAsync(
       [FromServices] IGetService<CreateDepositFormCommand, Guid> createDepositFormService,
       [FromServices] IGetService<GetBrandPaymentQuery, BrandPaymentInfo> getBrandPaymentService,
       [FromServices] ManualDepositSerializer serializer,
       [FromBody] SubmitManualDepositFpsRequest request)
    {
        var playerTokenData = HttpContext.Items.GetPlayerTokenData();
        var brandId = HttpContext.Items.GetBrandId();

        var brandPayment = await getBrandPaymentService
            .GetAsync(new(request.BrandPaymentId), HttpContext.RequestAborted)
            .ConfigureAwait(false);

        var brandPaymentBankInfo = JsonDocument.Parse(brandPayment.ExtendedData);

        var qrCodeFileUrl = JsonElementExtenstion.GetPropertyValue(brandPaymentBankInfo.RootElement, "qrCodeFileUrl");
        var email = JsonElementExtenstion.GetPropertyValue(brandPaymentBankInfo.RootElement, "email");
        var fpsId = JsonElementExtenstion.GetPropertyValue(brandPaymentBankInfo.RootElement, "fpsId");
        var phone = JsonElementExtenstion.GetPropertyValue(brandPaymentBankInfo.RootElement, "phone");

        var command = new CreateDepositFormCommand(
            playerTokenData.PlayerCode,
            brandId,
            request.BrandPaymentId,
            request.Amount,
            "tgs.payment.manual.deposit",
            request.PromotionId,
            _ => serializer.Serialize(
                new ManualDepositFpsApplyInfo
                {
                    SendBankAccount = request.BankAccount,
                    SendBankName = request.BankName,
                    BankType = request.BankType,
                    QrCodeFileUrl = qrCodeFileUrl,
                    Email = email,
                    FpsId = fpsId,
                    Phone = phone
                }));

        return await createDepositFormService
            .GetAsync(command, HttpContext.RequestAborted)
            .ConfigureAwait(false);
    }

    [HttpPut("pulsa", Name = "SubmitDepositPulsaRequest")]
    [EnableUserBlockedCheck]
    [AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
    public async ValueTask<Guid> SubmitDepositPulsaRequestAsync(
       [FromServices] IGetService<CreateDepositFormCommand, Guid> createDepositFormService,
       [FromServices] IGetService<GetBrandPaymentQuery, BrandPaymentInfo> getBrandPaymentService,
       [FromServices] ManualDepositSerializer serializer,
       [FromBody] SubmitManualDepositPulsaRequest request)
    {
        var playerTokenData = HttpContext.Items.GetPlayerTokenData();
        var brandId = HttpContext.Items.GetBrandId();

        var brandPayment = await getBrandPaymentService
            .GetAsync(new(request.BrandPaymentId), HttpContext.RequestAborted)
            .ConfigureAwait(false);

        var brandPaymentBankInfo = JsonDocument.Parse(brandPayment.ExtendedData);

        var command = new CreateDepositFormCommand(
            playerTokenData.PlayerCode,
            brandId,
            request.BrandPaymentId,
            request.Amount,
            "tgs.payment.manual.deposit",
            request.PromotionId,
            _ => serializer.Serialize(
                new ManualDepositPulsaApplyInfo
                {
                    Phone = brandPaymentBankInfo.RootElement.GetProperty("phone").GetString(),
                    SendNote = request.Note,
                    BankType = request.BankType,
                }));

        return await createDepositFormService
            .GetAsync(command, HttpContext.RequestAborted)
            .ConfigureAwait(false);
    }

    [HttpPut("thirdparty")]
    [EnableUserBlockedCheck]
    [AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
    public async ValueTask<Guid> SubmitDepositThirdPartyRequestAsync(
        [FromServices] IGetService<CreateDepositFormCommand, Guid> createDepositFormService,
        [FromServices] IGetService<GetBrandPaymentQuery, BrandPaymentInfo> getBrandPaymentService,
        [FromServices] ManualDepositSerializer serializer,
        [FromBody] SubmitManualDepositThirdPartyRequest request)
    {
        var playerTokenData = HttpContext.Items.GetPlayerTokenData();
        var brandId = HttpContext.Items.GetBrandId();

        var brandPayment = await getBrandPaymentService
            .GetAsync(new(request.BrandPaymentId), HttpContext.RequestAborted)
            .ConfigureAwait(false);

        var brandPaymentBankInfo = JsonDocument.Parse(brandPayment.ExtendedData);

        var command = new CreateDepositFormCommand(
            playerTokenData.PlayerCode,
            brandId,
            request.BrandPaymentId,
            request.Amount,
            "tgs.payment.manual.deposit",
            request.PromotionId,
            _ => serializer.Serialize(
                new ManualDepositThirdPartyApplyInfo
                {
                    BankType = request.BankType,
                    QrCode = brandPaymentBankInfo.RootElement.GetProperty("qrCode").GetString()
                }));

        return await createDepositFormService
            .GetAsync(command, HttpContext.RequestAborted)
            .ConfigureAwait(false);
    }

    [HttpPut("usdt", Name = "SubmitDepositUsdtRequest")]
    [EnableUserBlockedCheck]
    [AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
    public async ValueTask<Guid> SubmitDepositUsdtRequestAsync(
       [FromServices] IGetService<CreateDepositFormCommand, Guid> createDepositFormService,
       [FromServices] IGetService<GetBrandPaymentQuery, BrandPaymentInfo> getBrandPaymentService,
       [FromServices] ManualDepositSerializer serializer,
       [FromBody] SubmitManualDepositUsdtRequest request)
    {
        var playerTokenData = HttpContext.Items.GetPlayerTokenData();
        var brandId = HttpContext.Items.GetBrandId();

        var brandPayment = await getBrandPaymentService
            .GetAsync(new(request.BrandPaymentId), HttpContext.RequestAborted)
            .ConfigureAwait(false);

        var brandPaymentBankInfo = JsonDocument.Parse(brandPayment.ExtendedData);

        var rate = brandPaymentBankInfo.RootElement.GetProperty("rate").GetDecimal();

        var command = new CreateDepositFormCommand(
            playerTokenData.PlayerCode,
            brandId,
            request.BrandPaymentId,
            request.Amount / rate,
            "tgs.payment.manual.deposit",
            request.PromotionId,
            _ => serializer.Serialize(
                new ManualDepositUsdtApplyInfo
                {
                    BankType = request.BankType,
                    Address = brandPaymentBankInfo.RootElement.GetProperty("address").GetString(),
                    QrCode = brandPaymentBankInfo.RootElement.GetProperty("qrCode").GetString(),
                    Rate = rate,
                    SendAmount = request.Amount
                }));

        return await createDepositFormService
            .GetAsync(command, HttpContext.RequestAborted)
            .ConfigureAwait(false);
    }

    private ManualDepositApplyInfo GetApplyInfo(ManualDepositSerializer serializer, string bankType, string applyInfo)
    {
        return bankType switch
        {
            "Bank" => serializer.Deserialize<ManualDepositBankApplyInfo>(applyInfo),
            "Fps" => serializer.Deserialize<ManualDepositFpsApplyInfo>(applyInfo),
            "ThirdParty" => serializer.Deserialize<ManualDepositThirdPartyApplyInfo>(applyInfo),
            _ => throw new NotImplementedException()
        };
    }
}
