using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgs.BrandCertificate.Clients;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;
using TGS.Services.Operations;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class CreatePlayerCommandService : IGetService<CreatePlayerCommand, CreatePlayerInfo>
{
	private readonly IBrandOperations _brandOperations;
	private readonly IBrandServiceClient _brandServiceClient;
	private readonly IPlayerServiceClient _playerServiceClient;

	public CreatePlayerCommandService(
		IBrandOperations brandOperations,
		IPlayerServiceClient playerServiceClient,
		IBrandServiceClient brandServiceClient)
	{
		_brandOperations = brandOperations ?? throw new ArgumentNullException(nameof(brandOperations));
		_playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
		_brandServiceClient = brandServiceClient ?? throw new ArgumentNullException(nameof(brandServiceClient));
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

		var betLimitId = await _brandOperations
			.GetBrandDefaultBetLimitAsync(command.BrandId, command.CurrencyCode)
			.ConfigureAwait(false);

		var vipInfo = await _brandServiceClient
			.GetVipLevelInfosAsync(command.BrandId, cancellationToken)
			.FirstOrDefaultAsync(x => x.Level == 1, cancellationToken)
			.ConfigureAwait(false);

		var resultPlayerCode = await CreatePlayerAsync(
			command,
			command.CurrencyCode,
			betLimitId,
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
