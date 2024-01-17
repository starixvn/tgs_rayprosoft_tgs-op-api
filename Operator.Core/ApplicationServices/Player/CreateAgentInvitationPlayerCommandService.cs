using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgs.BrandCertificate.Clients;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;
using Tgs.VipLevelService.Clients;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Exceptions;
using TGS.Services.Operations;
using PlayerType = Tgs.PlayerService.Client.PlayerType;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class CreateAgentInvitationPlayerCommandService
	: IGetService<CreatePlayerWithAgentInvitationCodeCommand, CreatePlayerInfo>
{
	private readonly IAgentRepository _agentRepository;
	private readonly IBrandServiceClient _brandServiceClient;
	private readonly IVipLevelServiceClient _vipLevelServiceClient;
	private readonly IPlayerServiceClient _playerServiceClient;

	public CreateAgentInvitationPlayerCommandService(
		IAgentRepository agentRepository,
		IPlayerServiceClient playerServiceClient,
		IBrandServiceClient brandServiceClient,
		IVipLevelServiceClient vipLevelServiceClient)
	{
		_agentRepository = agentRepository ?? throw new ArgumentNullException(nameof(agentRepository));
		_playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
		_brandServiceClient = brandServiceClient ?? throw new ArgumentNullException(nameof(brandServiceClient));
		_vipLevelServiceClient = vipLevelServiceClient ?? throw new ArgumentNullException(nameof(vipLevelServiceClient));
	}

	public async ValueTask<CreatePlayerInfo> GetAsync(
		CreatePlayerWithAgentInvitationCodeCommand command,
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

		var resultPlayerCode = await CreatePlayerByAgentAsync(
			command,
			command.CurrencyCode,
			betLimitId.Value,
			vipInfo!.Id).ConfigureAwait(false);

		return new CreatePlayerInfo(resultPlayerCode, true);
	}

	private async Task<string> CreatePlayerByAgentAsync(
		CreatePlayerWithAgentInvitationCodeCommand command,
		string currency,
		int betLimitId,
		Guid vipId)
	{
		var agent = await _agentRepository
			.GetAgentInfoByInvitationCodeAsync(command.AgentInvitationCode)
			.ConfigureAwait(false);

		if (agent is not { Status: AgentStatus.Enabled })
			throw new InvalidInvitationCodeException(command.AgentInvitationCode, "Can't find the agent info");

		var result = await _playerServiceClient.CreatePlayerAsync(
			new CreatePlayerData(
				command.BrandId,
				agent.Id,
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

		return result;
	}
}
