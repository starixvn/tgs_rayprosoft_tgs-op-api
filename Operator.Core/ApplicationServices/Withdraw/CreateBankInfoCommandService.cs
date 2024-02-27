using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Payment.Clients;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Withdraw;

internal class CreateBankInfoCommandService(IPaymentServiceClient paymentServiceClient) : ICommandService<CreateBankInfoCommand>
{
	private readonly IPaymentServiceClient _paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));

	public async ValueTask ExecuteAsync(CreateBankInfoCommand command, CancellationToken cancellationToken = default)
	{
		var bankInfo = new BankCreateInfo
		{
			BankType = command.BankType,
			PaymentChannelId = command.PaymentChannelId,
			Detail = command.Detail
		};

		_ = await _paymentServiceClient
			 .CreatePlayerBankInfoAsync(
				 command.BrandId,
				 command.PlayerCode,
				 bankInfo,
				 cancellationToken: cancellationToken)
			 .ConfigureAwait(false);
	}
}