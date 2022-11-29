using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgs.BrandCertificate.Clients;
using TGS.Core.Models.Exceptions;
using Tgs.PlayerService.Client;
using TGS.Services.Operations;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class CreatePlayerInvitationPlayerCommandService : IGetService<CreatePlayerWithPlayerInvitationCommand, CreatePlayerInfo>
{
	private readonly IBrandOperations _brandOperations;
	private readonly IBrandServiceClient _brandServiceClient;
	private readonly IPlayerServiceClient _playerServiceClient;

	public CreatePlayerInvitationPlayerCommandService(
		IBrandOperations brandOperations,
		IPlayerServiceClient playerServiceClient,
		IBrandServiceClient brandServiceClient)
	{
		_brandOperations = brandOperations ?? throw new ArgumentNullException(nameof(brandOperations));
		_playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
		_brandServiceClient = brandServiceClient ?? throw new ArgumentNullException(nameof(brandServiceClient));
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

		var betLimitId = await _brandOperations
			.GetBrandDefaultBetLimitAsync(command.BrandId, command.CurrencyCode)
			.ConfigureAwait(false);

		var vipInfo = await _brandServiceClient
			.GetVipLevelInfosAsync(command.BrandId, cancellationToken)
			.FirstOrDefaultAsync(x => x.Level == 1, cancellationToken)
			.ConfigureAwait(false);

		var resultPlayerCode = await CreatePlayerByUpperPlayerAsync(
			command,
			command.CurrencyCode,
			betLimitId,
			vipInfo!.Id,
			cancellationToken).ConfigureAwait(false);

		return new CreatePlayerInfo(resultPlayerCode, true);
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
				RegisterPlatformType.Player,
				command.UserName), cancellationToken);

		return result;
	}
}
