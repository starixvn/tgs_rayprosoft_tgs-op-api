using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.BrandCertificate.Clients;
using Tgs.Payment.Clients;
using Tgs.ServiceUtility;
using TGS.Core.Infrastructures.Redis;
using TGS.Core.Models.Enums;

namespace TGS.Services.Operator.ApplicationServices.Withdraw;

internal class CreateInitBankInfoCommandService : ICommandService<CreateInitBankInfoCommand>
{
    private readonly IRedisCacheManager _cacheManager;
    private readonly IPaymentServiceClient _paymentServiceClient;

    public CreateInitBankInfoCommandService(
        IPaymentServiceClient paymentServiceClient,
        IRedisCacheManager cacheManager)
    {
        _paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));
        _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
    }

    public async ValueTask ExecuteAsync(
        CreateInitBankInfoCommand command,
        CancellationToken cancellationToken = default)
    {
        var playerCode = command.PlayerCode;

        var bankInfo = new BankCreateInfo
        {
            BankType = command.BankType,
            PaymentChannelId = command.PaymentChannelId,
            Detail = command.Detail
        };

        var bankInfoId = await _paymentServiceClient
            .CreatePlayerBankInfoAsync(
                command.BrandId,
                playerCode,
                bankInfo,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        await _paymentServiceClient
            .SetDefaultPlayerBankInfoAsync(
                playerCode,
                bankInfoId,
                playerCode,
                cancellationToken)
            .ConfigureAwait(false);

        await _cacheManager
            .ClearCacheAsync(RedisCacheType.Player, playerCode)
            .ConfigureAwait(false);
    }
}
