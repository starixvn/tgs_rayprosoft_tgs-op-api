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
	private readonly IPaymentServiceClient _paymentServiceClient;
	private readonly IPlayerBucketClient _playerBucketClient;
	private readonly IPlayerServiceClient _playerServiceClient;

	public CreateWithdrawFormCommandService(
		IPaymentServiceClient paymentServiceClient,
		IPlayerServiceClient playerServiceClient,
		IPlayerBucketClient playerBucketClient)
	{
		_paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));
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
