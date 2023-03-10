using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgs.BrandCertificate.Clients;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class GetPlayerQueryService : IGetService<GetPlayerQuery, PlayerInfo>
{
	private readonly IBrandServiceClient _brandServiceClient;
	private readonly IPlayerServiceClient _playerServiceClient;

	public GetPlayerQueryService(
		IPlayerServiceClient playerServiceClient,
		IBrandServiceClient brandServiceClient)
	{
		_playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
		_brandServiceClient = brandServiceClient ?? throw new ArgumentNullException(nameof(brandServiceClient));
	}

	public async ValueTask<PlayerInfo> GetAsync(
		GetPlayerQuery query,
		CancellationToken cancellationToken = default)
	{
		var player = await _playerServiceClient
			.GetPlayerInfoAsync(query.PlayerCode, cancellationToken)
			.ConfigureAwait(false) 
			?? throw new PlayerNotFoundException("Player not found.");

		var playerProperties = await _playerServiceClient
			.GetPlayerPropertiesAsync(query.PlayerCode, cancellationToken)
			.ConfigureAwait(false);

		var depositedAmount = await _playerServiceClient
			.GetPlayerDepositedAmountAsync(query.PlayerCode, cancellationToken)
			.ConfigureAwait(false);

		var brandVipLevels = await _brandServiceClient
			.GetVipLevelInfosAsync(player.BrandId, cancellationToken)
			.ToDictionaryAsync(info => info.Id, info => info.Level, cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		var playerStatus = await _playerServiceClient
			.GetPlayerStatusAsync(query.PlayerCode, cancellationToken)
			.ConfigureAwait(false);

		return new PlayerInfo(
			player.PlayerCode,
			player.BrandId,
			depositedAmount,
			player.IdentityAuthentication,
			player.PhoneAuthentication,
			playerProperties.GetValueOrDefault("Phone"),
			playerProperties.GetValueOrDefault("AreaCode"),
			player.VipLevelId,
			playerStatus,
			player.UserName,
			playerProperties["RealName"],
			playerProperties["Email"],
			player.VipLevelId is null ? 1 : brandVipLevels.GetValueOrDefault(player.VipLevelId.Value),
			playerProperties.GetValueOrDefault("QQ"),
			playerProperties.GetValueOrDefault("Wechat"),
			player.IsSetPayPassword);
	}
}
