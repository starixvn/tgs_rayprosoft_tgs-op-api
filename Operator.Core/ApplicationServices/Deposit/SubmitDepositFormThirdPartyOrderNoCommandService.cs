using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Payment.Clients;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Deposit;

internal class SubmitDepositFormThirdPartyOrderNoCommandService : ICommandService<SubmitDepositFormThirdPartyOrderNoCommand>
{
    private readonly IPaymentServiceClient _paymentServiceClient;

    public SubmitDepositFormThirdPartyOrderNoCommandService(IPaymentServiceClient paymentServiceClient)
    {
        _paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));
    }

    public async ValueTask ExecuteAsync(SubmitDepositFormThirdPartyOrderNoCommand command, CancellationToken cancellationToken = default)
    {
        var formData = await _paymentServiceClient
            .GetDepositFormAsync(command.FormId)
            .ConfigureAwait(false);

        if (formData == null)
            return;

        await _paymentServiceClient
            .SubmitDepositFormStatusAsync(
                command.FormId,
                formData.Status,
                thirdPartyOrderNo: command.ThirdPartyOrderNo)
            .ConfigureAwait(false);
    }
}
