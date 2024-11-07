using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Tgs.BrandCertificate.Clients;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Player;

public class RegisterByPhoneCommandService(
	IBrandServiceClient brandServiceClient,
	IGetService<CreatePlayerWithPlayerInvitationCommand, CreatePlayerInfo> createPlayerInvitationPlayerService,
	IPlayerServiceClient playerServiceClient,
	IGetService<CreatePlayerWithAgentInvitationCodeCommand, CreatePlayerInfo> createAgentInvitationPlayerService,
	IGetService<CreatePlayerCommand, CreatePlayerInfo> createPlayerService)
	: ICommandService<RegisterByPhoneCommand>
{
	public async ValueTask ExecuteAsync(RegisterByPhoneCommand query, CancellationToken cancellationToken = new CancellationToken())
	{
		var brand = await brandServiceClient
			.GetBrandInfoAsync(query.BrandId, cancellationToken)
			.ConfigureAwait(false);

		CreatePlayerInfo? info = null;
		if (!string.IsNullOrWhiteSpace(query.UpperPlayerCode))
		{
			var command = new CreatePlayerWithPlayerInvitationCommand(query.BrandId,
				query.UserName,
				query.Password,
				brand!.Currency,
				query.UpperPlayerCode, false);

			info = await createPlayerInvitationPlayerService
				.GetAsync(command, cancellationToken)
				.ConfigureAwait(false);
		}
		else if (!string.IsNullOrWhiteSpace(query.AgentInvitationCode))
		{
			var command = new CreatePlayerWithAgentInvitationCodeCommand(query.BrandId,
				query.UserName,
				query.Password,
				brand!.Currency,
				query.AgentInvitationCode,
				false);

			info = await createAgentInvitationPlayerService
				.GetAsync(command, cancellationToken)
				.ConfigureAwait(false);
		}
		else
		{
			var createPlayerCommand
				= new CreatePlayerCommand(query.BrandId,
					query.UserName,
					query.Password,
					brand!.Currency,
					false);

			info = await createPlayerService.GetAsync(createPlayerCommand, cancellationToken).ConfigureAwait(false);
		}

		if (info!.IsNew == false)
		{
			throw new PlayerUserNameDuplicateException();
		}

		await playerServiceClient.SetPlayerPropertiesAsync(
				info.PlayerCode,
				new Dictionary<PlayerPropertyName, string?>
				{
					[PlayerPropertyName.AreaCode] = query.AreaCode,
					[PlayerPropertyName.Phone] = query.Phone,
					[PlayerPropertyName.PhoneAuthentication] = true.ToString()
				}.ToImmutableDictionary(),
				null,
				cancellationToken)
			.ConfigureAwait(false);

		await playerServiceClient.SetPlayerStatusAsync(info.PlayerCode,
			PlayerStatus.Enabled,
			null,
			cancellationToken).ConfigureAwait(false);

		await playerServiceClient.RecordRegisterIpAsync(
			info.PlayerCode,
			query.IpAddress,
			query.HostName,
			cancellationToken).ConfigureAwait(false);
	}
}