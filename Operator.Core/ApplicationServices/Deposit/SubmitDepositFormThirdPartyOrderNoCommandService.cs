using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Payment.Clients;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Deposit;

internal class SubmitDepositFormThirdPartyOrderNoCommandService(IPaymentServiceClient paymentServiceClient) : ICommandService<SubmitDepositFormThirdPartyOrderNoCommand>
{
	private readonly IPaymentServiceClient _paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));

	public async ValueTask ExecuteAsync(SubmitDepositFormThirdPartyOrderNoCommand command, CancellationToken cancellationToken = default)
	{
		var formData = await _paymentServiceClient
			.GetDepositFormAsync(command.FormId, cancellationToken)
			.ConfigureAwait(false);

		if (formData == null)
			return;

		await _paymentServiceClient
			.SubmitDepositFormStatusAsync(
				command.FormId,
				formData.Status,
				thirdPartyOrderNo: command.ThirdPartyOrderNo,
				cancellationToken: cancellationToken)
			.ConfigureAwait(false);
	}
}
