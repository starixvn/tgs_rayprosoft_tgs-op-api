using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.CashService.Client;
using Tgs.Payment.Clients;
using Tgs.PlayerService.Client;
using Tgs.Service.PlayerBuckets;
using Tgs.ServiceUtility;
using TGS.Services.Operator.BrandPayment;

namespace TGS.Services.Operator.ApplicationServices.Withdraw;

internal class CreateWithdrawFormCommandService : ICommandService<CreateWithdrawFormCommand>
{
	private readonly ICashServiceClient _cashServiceClient;
	private readonly IPaymentServiceClient _paymentServiceClient;
	private readonly IPlayerServiceClient _playerServiceClient;
	private readonly IPlayerBucketClient _playerBucketClient;

	public CreateWithdrawFormCommandService(
		IPaymentServiceClient paymentServiceClient,
		ICashServiceClient cashServiceClient,
		IPlayerServiceClient playerServiceClient,
		IPlayerBucketClient playerBucketClient)
	{
		_paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));
		_cashServiceClient = cashServiceClient ?? throw new ArgumentNullException(nameof(cashServiceClient));
		_playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
		_playerBucketClient = playerBucketClient ?? throw new ArgumentNullException(nameof(playerBucketClient));
	}

	public async ValueTask ExecuteAsync(CreateWithdrawFormCommand command, CancellationToken cancellationToken = default)
	{
		var playerInfo = await _playerServiceClient
			.GetPlayerInfoAsync(command.PlayerCode, cancellationToken)
			.ConfigureAwait(false)
			?? throw new PlayerNotFoundException("Can't find player.");

		var bucketResult = await _playerBucketClient.ReadBucketsAsync(
			command.PlayerCode,
			cancellationToken).ConfigureAwait(false);

		if (bucketResult.Amount > 0M)
			throw new InsufficientBucketException();

		var playerProps = await _playerServiceClient
			.GetPlayerPropertiesAsync(command.PlayerCode, cancellationToken)
			.ConfigureAwait(false);

		var playerWalletId = Guid.Parse(playerProps["DefaultWalletId"]!);

		var balance = await _cashServiceClient
			.GetBalanceAsync(playerWalletId, cancellationToken)
			.ConfigureAwait(false);

		if (balance - command.Amount < 0)
			throw new InsufficientFundsException();

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

		var result = await _cashServiceClient
			.LockWithdrawAsync(
				playerWalletId,
				command.Amount,
				null,
				createFormId.ToString(),
				cancellationToken)
			.ConfigureAwait(false);

		if (result.Balance < 0)
		{
			_ = await _cashServiceClient.RollbackWithdrawAsync(
				playerWalletId,
				command.Amount,
				null,
				createFormId.ToString(),
				null,
				cancellationToken).ConfigureAwait(false);

			await _paymentServiceClient
				.SubmitWithdrawFormStatusAsync(
					createFormId,
					PaymentFlowState.Reject,
					cancellationToken: cancellationToken)
				.ConfigureAwait(false);

			command.ReturnFormIdSource.SetResult(createFormId);

			throw new InsufficientFundsException();
		}

		await _paymentServiceClient
			.SubmitWithdrawFormStatusAsync(createFormId, PaymentFlowState.WaitApprove, cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		command.ReturnFormIdSource.SetResult(createFormId);
	}
}
