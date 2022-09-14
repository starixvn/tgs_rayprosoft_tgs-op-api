using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.CashService.Client;
using Tgs.Payment.Clients;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;
using TGS.Services.Operator.BrandPayment;
using TGS.Services.PlayerBucket;

namespace TGS.Services.Operator.ApplicationServices.Withdraw;

internal class CreateWithdrawFormCommandService : ICommandService<CreateWithdrawFormCommand>
{
    private readonly ICashServiceClient _cashServiceClient;
    private readonly IPaymentServiceClient _paymentServiceClient;
    private readonly IPlayerBucketRepository _playerBucketRepository;
    private readonly IPlayerServiceClient _playerServiceClient;

    public CreateWithdrawFormCommandService(
        IPaymentServiceClient paymentServiceClient,
        ICashServiceClient cashServiceClient,
        IPlayerServiceClient playerServiceClient,
        IPlayerBucketRepository playerBucketRepository)
    {
        _paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));
        _cashServiceClient = cashServiceClient ?? throw new ArgumentNullException(nameof(cashServiceClient));
        _playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
        _playerBucketRepository = playerBucketRepository ?? throw new ArgumentNullException(nameof(playerBucketRepository));
    }

    public async ValueTask ExecuteAsync(CreateWithdrawFormCommand command, CancellationToken cancellationToken = default)
    {
        var playerInfo = await _playerServiceClient
            .GetPlayerInfoAsync(command.PlayerCode)
            .ConfigureAwait(false);

        if (playerInfo is null)
            throw new PlayerNotFoundException("Can't find player.");

        var isBucketFull = await _playerBucketRepository
            .IsBucketFullAsync(command.PlayerCode)
            .ConfigureAwait(false);

        if (!isBucketFull)
            throw new InsufficientBucketException();

        var playerProps = await _playerServiceClient
            .GetPlayerPropertiesAsync(command.PlayerCode)
            .ConfigureAwait(false);

        var playerWalletId = Guid.Parse(playerProps["DefaultWalletId"]!);

        var balance = await _cashServiceClient
            .GetBalanceAsync(playerWalletId)
            .ConfigureAwait(false);

        if (balance - command.Amount < 0)
            throw new InsufficientFundsException();

        var createFormId = await _paymentServiceClient
            .CreateWithdrawFormAsync(
                new WithdrawFormCreateInfo
                {
                    Amount = command.Amount,
                    BrandId = command.BrandId,
                    BankInfoId = command.BankInfoId,
                    UserId = command.PlayerCode,
                    UserName = playerInfo.UserName,
                    UserType = UserType.Player,
                    Note = command.Note,
                })
            .ConfigureAwait(false);

        var result = await _cashServiceClient
            .LockWithdrawAsync(
                playerWalletId,
                command.Amount,
                null,
                createFormId.ToString())
            .ConfigureAwait(false);

        if (result.Balance < 0)
        {
            _ = await _cashServiceClient.RollbackWithdrawAsync(
                playerWalletId,
                command.Amount,
                null,
                createFormId.ToString(),
                null).ConfigureAwait(false);

            await _paymentServiceClient
                .SubmitWithdrawFormStatusAsync(
                    createFormId,
                    PaymentFlowState.Reject)
                .ConfigureAwait(false);

            command.ReturnFormIdSource.SetResult(createFormId);

            throw new InsufficientFundsException();
        }

        await _paymentServiceClient
            .SubmitWithdrawFormStatusAsync(
                createFormId,
                PaymentFlowState.WaitApprove)
            .ConfigureAwait(false);

        command.ReturnFormIdSource.SetResult(createFormId);
    }
}
