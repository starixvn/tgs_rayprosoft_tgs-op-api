using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgs.BrandCertificate.Clients;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;
using Tgs.VipLevelService.Clients;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class CreatePlayerCommandService : IGetService<CreatePlayerCommand, CreatePlayerInfo>
{
	private readonly IBrandServiceClient _brandServiceClient;
	private readonly IVipLevelServiceClient _vipLevelServiceClient;
	private readonly IPlayerServiceClient _playerServiceClient;

	public CreatePlayerCommandService(
		IPlayerServiceClient playerServiceClient,
		IBrandServiceClient brandServiceClient,
		IVipLevelServiceClient vipLevelServiceClient)
	{
		_playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
		_brandServiceClient = brandServiceClient ?? throw new ArgumentNullException(nameof(brandServiceClient));
		_vipLevelServiceClient = vipLevelServiceClient ?? throw new ArgumentNullException(nameof(vipLevelServiceClient));
	}

	public async ValueTask<CreatePlayerInfo> GetAsync(
		CreatePlayerCommand command,
		CancellationToken cancellationToken = default)
	{
		var player = await _playerServiceClient
			.GetPlayerInfoByUserNameAsync(command.BrandId, command.UserName, cancellationToken)
			.ConfigureAwait(false);

		if (player != null)
			return new CreatePlayerInfo(player.PlayerCode, false);

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
			.GetBrandAllVipLevelsAsync(command.BrandId, cancellationToken)
			.FirstOrDefaultAsync(x => x.Level == 1, cancellationToken)
			.ConfigureAwait(false);

		var resultPlayerCode = await CreatePlayerAsync(
			command,
			command.CurrencyCode,
			betLimitId.Value,
			vipInfo!.Id).ConfigureAwait(false);

		return new CreatePlayerInfo(resultPlayerCode, true);
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
				null,
				betLimitId,
				command.IsTestPlayer
					? PlayerType.TestPlayer
					: PlayerType.RealPlayer,
				RegisterSiteType.Player,
				command.UserName));
}
