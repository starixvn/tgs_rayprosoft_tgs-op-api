using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Payment.Clients;
using Tgs.ServiceUtility;
using TGS.Core.Infrastructures.Redis;
using TGS.Core.Models.Enums;

namespace TGS.Services.Operator.ApplicationServices.Withdraw;

internal class CreateInitBankInfoCommandService(
	IPaymentServiceClient paymentServiceClient,
	IRedisCacheManager cacheManager) : ICommandService<CreateInitBankInfoCommand>
{
	private readonly IRedisCacheManager _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
	private readonly IPaymentServiceClient _paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));

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
