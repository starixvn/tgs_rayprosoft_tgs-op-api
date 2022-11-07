using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.BrandCertificate.Clients;
using Tgs.Core.Infrastructures.Security;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Exceptions;
using TGS.Services.Operations;
using TGS.Services.Operations.Models.Services;
using PlayerStatus = Tgs.PlayerService.Client.PlayerStatus;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class CreateAgentInvitationPlayerCommandService : IGetService<CreatePlayerWithAgentInvitationCodeCommand, CreatePlayerInfo>
{
	private readonly IAgentRepository _agentRepository;
	private readonly IBrandOperations _brandOperations;
	private readonly IBrandServiceClient _brandServiceClient;
	private readonly IPasswordHandler _passwordHandler;
	private readonly IPlayerOperations _playerOperations;
	private readonly IPlayerServiceClient _playerServiceClient;

	public CreateAgentInvitationPlayerCommandService(
		IPlayerOperations playerOperations,
		IPasswordHandler passwordHandler,
		IBrandOperations brandOperations,
		IAgentRepository agentRepository,
		IPlayerServiceClient playerServiceClient,
		IBrandServiceClient brandServiceClient)
	{
		_playerOperations = playerOperations ?? throw new ArgumentNullException(nameof(playerOperations));
		_passwordHandler = passwordHandler ?? throw new ArgumentNullException(nameof(passwordHandler));
		_brandOperations = brandOperations ?? throw new ArgumentNullException(nameof(brandOperations));
		_agentRepository = agentRepository ?? throw new ArgumentNullException(nameof(agentRepository));
		_playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
		_brandServiceClient = brandServiceClient ?? throw new ArgumentNullException(nameof(brandServiceClient));
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

		var (salt, password) = _passwordHandler.GenerateCredential(command.Password);

		_ = await _brandServiceClient
			.GetBrandInfoAsync(command.BrandId, cancellationToken)
			.ConfigureAwait(false) ?? throw new BrandNotFoundException();

		var betLimitId = await _brandOperations
			.GetBrandDefaultBetLimitAsync(command.BrandId, command.CurrencyCode)
			.ConfigureAwait(false);

		var resultPlayerCode = await CreatePlayerByAgentAsync(
			command,
			salt!,
			password!,
			command.CurrencyCode,
			betLimitId,
			PlayerStatus.RegisterUndone).ConfigureAwait(false);

		return new CreatePlayerInfo(resultPlayerCode, true);
	}

	private async Task<string> CreatePlayerByAgentAsync(
		CreatePlayerWithAgentInvitationCodeCommand command,
		byte[] salt,
		byte[] password,
		string currency,
		int betLimitId,
		PlayerStatus status)
	{
		var agent = await _agentRepository
			.GetAgentInfoByInvitationCodeAsync(command.AgentInvitationCode)
			.ConfigureAwait(false);

		if (agent is not { Status: AgentStatus.Enabled })
			throw new InvalidInvitationCodeException(command.AgentInvitationCode, "Can't find the agent info");

		var result = await _playerOperations
			.CreatePlayerAsync(
				new CreatePlayerData
				{
					BrandId = command.BrandId,
					UserName = command.UserName,
					Salt = salt,
					Password = password,
					CurrencyCode = currency,
					BetLimitId = betLimitId,
					AgentId = agent.Id,
					AgentInvitationCode = command.AgentInvitationCode,
					Type = command.IsTestPlayer
						? Core.Models.Enums.PlayerType.TestPlayer
						: Core.Models.Enums.PlayerType.RealPlayer,
					PlayerStatus = (Core.Models.Enums.PlayerStatus)status
				})
			.ConfigureAwait(false);

		return result;
	}
}
