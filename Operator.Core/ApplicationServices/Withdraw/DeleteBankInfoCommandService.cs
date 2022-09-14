using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Payment.Clients;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Withdraw;

internal class DeleteBankInfoCommandService : ICommandService<DeleteBankInfoCommand>
{
    private readonly IPaymentServiceClient _paymentServiceClient;

    public DeleteBankInfoCommandService(IPaymentServiceClient paymentServiceClient)
    {
        _paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));
    }

    public ValueTask ExecuteAsync(DeleteBankInfoCommand command, CancellationToken cancellationToken = default)
        => _paymentServiceClient.DeletePlayerBankInfoAsync(command.PlayerCode, command.BankInfoId);
}
