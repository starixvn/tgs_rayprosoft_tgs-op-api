using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.BrandCertificate.Clients;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;
using Tgs.VipLevelService.Clients;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class CreatePlayerCommandService(
	IPlayerServiceClient playerServiceClient,
	IBrandServiceClient brandServiceClient,
	IVipLevelServiceClient vipLevelServiceClient) : IGetService<CreatePlayerCommand, CreatePlayerInfo>
{
	private readonly IBrandServiceClient _brandServiceClient = brandServiceClient ?? throw new ArgumentNullException(nameof(brandServiceClient));
	private readonly IVipLevelServiceClient _vipLevelServiceClient = vipLevelServiceClient ?? throw new ArgumentNullException(nameof(vipLevelServiceClient));
	private readonly IPlayerServiceClient _playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));

	public async ValueTask<CreatePlayerInfo> GetAsync(
		CreatePlayerCommand command,
		CancellationToken cancellationToken = default)
	{
		var player = await _playerServiceClient
			.GetPlayerInfoByUserNameAsync(command.BrandId, command.UserName, cancellationToken)
			.ConfigureAwait(false);

		if (player != null)
			return new CreatePlayerInfo(player.PlayerCode, false, "username-existed");

		var isEmailValid = await _playerServiceClient
		.CheckPlayerInfoEmailAsync(command.Email, cancellationToken)
		.ConfigureAwait(false);

		if (isEmailValid == false)
			return new CreatePlayerInfo("", false, "email-invalid");

		var emailPlayer = await _playerServiceClient
			.GetPlayerInfoByEmailAsync(command.BrandId, command.Email, cancellationToken)
			.ConfigureAwait(false);

		if (emailPlayer != null)
			return new CreatePlayerInfo(emailPlayer.PlayerCode, false, "email-existed");


		_ = await _brandServiceClient
			.GetBrandInfoAsync(command.BrandId, cancellationToken)
			.ConfigureAwait(false) ?? throw new BrandNotFoundException();

		var betLimitId = await _brandServiceClient
			.GetBrandBetLimitIdAsync(command.BrandId, cancellationToken)
			.ConfigureAwait(false);

		if (!betLimitId.HasValue)
			throw new InvalidOperationException(
				$"Brand with id '{command.BrandId}' doesn't have default bet limit.");

		var vipInfo = await _vipLevelServiceClient
			.VipLevelGetAsync(command.BrandId, 1, cancellationToken)
			.ConfigureAwait(false)
			?? throw new NullReferenceException($"The VIP level for this brand does not exist.(1)");

		var resultPlayerCode = await CreatePlayerAsync(
			command,
			command.CurrencyCode,
			betLimitId.Value,
			vipInfo!.Id).ConfigureAwait(false);

		await _vipLevelServiceClient.PlayerVipLevelRegisterAsync(
			playerCode: resultPlayerCode,
			vipLevel: vipInfo.Level,
			autoState: Tgs.VipLevelService.Common.PlayerVipLevelAutoState.Enable,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		return new CreatePlayerInfo(resultPlayerCode, true, "valid");
	}

	private ValueTask<string> CreatePlayerAsync(
		CreatePlayerCommand command,
		string currency,
		int betLimitId,
		Guid vipId)
		=> _playerServiceClient.CreatePlayerAsync(
			new CreatePlayerData(
				command.BrandId,
				null,
				vipId,
				currency,
				command.UserName,
				command.Password,
				command.Email,
				null,
				betLimitId,
				command.IsTestPlayer
					? PlayerType.TestPlayer
					: PlayerType.RealPlayer,
				RegisterSiteType.Player,
				command.UserName));
}