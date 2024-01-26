using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class GetLogoutQueryService(
	IPlayerServiceClient playerServiceClient,
	IPlayerLoginRecordRepository loginRecordRepository) : IGetService<GetLogoutQuery, LogoutInfo>
{
	private readonly IPlayerLoginRecordRepository _loginRecordRepository = loginRecordRepository ?? throw new ArgumentNullException(nameof(loginRecordRepository));
	private readonly IPlayerServiceClient _playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));

	public async ValueTask<LogoutInfo> GetAsync(GetLogoutQuery query, CancellationToken cancellationToken = default)
	{
		_ = await _playerServiceClient
			.GetPlayerInfoAsync(query.PlayerCode, cancellationToken)
			.ConfigureAwait(false)
			?? throw new PlayerNotFoundException("");

		await _loginRecordRepository
			.DeauthorizePlayerAsync(query.LoginRecordId)
			.ConfigureAwait(false);

		return new LogoutInfo
		{
			UpdateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
		};
	}
}
