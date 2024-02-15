using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Payment.Clients;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Withdraw;

internal class CreateWithdrawFormCommandService(
	IPaymentServiceClient paymentServiceClient,
	IPlayerServiceClient playerServiceClient)
	: IGetService<CreateWithdrawFormCommand, Guid>
{
	public async ValueTask<Guid> GetAsync(CreateWithdrawFormCommand command, CancellationToken cancellationToken = default)
	{
		var playerInfo = await playerServiceClient
			.GetPlayerInfoAsync(command.PlayerCode, cancellationToken)
			.ConfigureAwait(false)
			?? throw new PlayerNotFoundException("Can't find player.");

		var createFormId = await paymentServiceClient
			.CreateWithdrawFormAsync(new WithdrawFormCreateInfo
			{
				Amount = command.Amount,
				BrandId = command.BrandId,
				BankInfoId = command.BankInfoId,
				UserId = command.PlayerCode,
				UserName = playerInfo.UserName,
				UserType = UserType.Player,
				Note = command.Note,
			}, cancellationToken)
			.ConfigureAwait(false);

		await paymentServiceClient.SubmitWithdrawFormStatusAsync(
			createFormId,
			PaymentFlowState.WaitApprove,
			cancellationToken: cancellationToken).ConfigureAwait(false);

		return createFormId;
	}
}
