using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.BrandCertificate.Clients;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Withdraw;

internal class CheckPlayerIdentityCommandService : IGetService<CheckPlayerIdentityCommand, bool>
{
	private readonly IBrandServiceClient _brandServiceClient;
	private readonly IPlayerServiceClient _playerServiceClient;

	public CheckPlayerIdentityCommandService(
		IBrandServiceClient brandServiceClient,
		IPlayerServiceClient playerServiceClient)
	{
		_brandServiceClient = brandServiceClient ?? throw new ArgumentNullException(nameof(brandServiceClient));
		_playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
	}

	public async ValueTask<bool> GetAsync(CheckPlayerIdentityCommand command, CancellationToken cancellationToken = default)
	{
		var brand = await _brandServiceClient
			.GetBrandInfoAsync(command.BrandId, cancellationToken)
			.ConfigureAwait(false);

		if (!brand!.PlayerIdentityEnabled)
			return true;

		var player = await _playerServiceClient
			.GetPlayerInfoAsync(command.PlayerCode, cancellationToken)
			.ConfigureAwait(false);

		return player == null ? throw new PlayerNotFoundException("Player not found when withdraw check.") : player.IdentityAuthentication;
	}
}
