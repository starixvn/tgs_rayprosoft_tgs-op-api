using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgs.BrandCertificate.Clients;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;
using Tgs.VipLevelService.Clients;
using TGS.Core.Models.Exceptions;
using TGS.Services.Operations;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class CreatePlayerInvitationPlayerCommandService : IGetService<CreatePlayerWithPlayerInvitationCommand, CreatePlayerInfo>
{
	private readonly IBrandServiceClient _brandServiceClient;
	private readonly IPlayerServiceClient _playerServiceClient;
	private readonly IVipLevelServiceClient _vipLevelServiceClient;

	public CreatePlayerInvitationPlayerCommandService(
		IPlayerServiceClient playerServiceClient,
		IBrandServiceClient brandServiceClient,
		IVipLevelServiceClient vipLevelServiceClient)
	{
		_playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
		_brandServiceClient = brandServiceClient ?? throw new ArgumentNullException(nameof(brandServiceClient));
		_vipLevelServiceClient = vipLevelServiceClient ?? throw new ArgumentNullException(nameof(vipLevelServiceClient));
	}

	private async Task<string> CreatePlayerByUpperPlayerAsync(
		CreatePlayerWithPlayerInvitationCommand command,
		string currency,
		int betLimitId,
		Guid vipId,
		CancellationToken cancellationToken)
	{
		var upperPlayer = await _playerServiceClient
			.GetPlayerInfoAsync(command.UpperPlayerCode, cancellationToken)
			.ConfigureAwait(false);

		var upperPlayerStatus = await _playerServiceClient
			.GetPlayerStatusAsync(command.UpperPlayerCode, cancellationToken)
			.ConfigureAwait(false);

		if (upperPlayer == null
			|| upperPlayer.BrandId != command.BrandId
			|| upperPlayerStatus != PlayerStatus.Enabled)
			throw new InvalidInvitationCodeException(
				command.UpperPlayerCode,
				"Can't find the player info");

		var result = await _playerServiceClient.CreatePlayerAsync(
			new CreatePlayerData(
				command.BrandId,
				null,
				vipId,
				currency,
				command.UserName,
				command.Password,
				upperPlayer.PlayerCode,
				betLimitId,
				command.IsTestPlayer
					? PlayerType.TestPlayer
					: PlayerType.RealPlayer,
				RegisterSiteType.Player,
				command.UserName), cancellationToken);

		return result;
	}

	public async ValueTask<CreatePlayerInfo> GetAsync(
		CreatePlayerWithPlayerInvitationCommand command,
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
			.VipLevelQueryAsync(command.BrandId, cancellationToken)
			.FirstOrDefaultAsync(x => x.Level == 1, cancellationToken)
			.ConfigureAwait(false);

		var resultPlayerCode = await CreatePlayerByUpperPlayerAsync(
			command,
			command.CurrencyCode,
			betLimitId.Value,
			vipInfo!.Id,
			cancellationToken).ConfigureAwait(false);

		return new CreatePlayerInfo(resultPlayerCode, true);
	}
}
