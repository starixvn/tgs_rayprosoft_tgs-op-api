using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Payment.Clients;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Withdraw;

internal class CreateWithdrawFormCommandService(
	IPaymentServiceClient paymentServiceClient,
	IPlayerServiceClient playerServiceClient) : ICommandService<CreateWithdrawFormCommand>
{
	private readonly IPaymentServiceClient _paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));
	private readonly IPlayerServiceClient _playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));

	public async ValueTask ExecuteAsync(CreateWithdrawFormCommand command, CancellationToken cancellationToken = default)
	{
		var playerInfo = await _playerServiceClient
			.GetPlayerInfoAsync(command.PlayerCode, cancellationToken)
			.ConfigureAwait(false)
			?? throw new PlayerNotFoundException("Can't find player.");

		var createFormId = await _paymentServiceClient
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

		await _paymentServiceClient
			.SubmitWithdrawFormStatusAsync(createFormId, PaymentFlowState.WaitApprove, cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		command.ReturnFormIdSource.SetResult(createFormId);
	}
}
