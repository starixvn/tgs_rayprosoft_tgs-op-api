using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;
using TGS.Core.Infrastructures.Mail;
using TGS.Services.Operations;
using TGS.Services.Operator.ApplicationServices.Auth;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class SetEmailCommandService(IPlayerOperations playerOperations, IPlayerServiceClient playerServiceClient) : IGetService<SetEmailCommand, SetEmailResponse>
{
	private readonly IPlayerOperations _playerOperations = playerOperations ?? throw new ArgumentNullException(nameof(playerOperations));
	private readonly IPlayerServiceClient _playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));

	public async ValueTask<SetEmailResponse> GetAsync(SetEmailCommand command, CancellationToken cancellationToken = default)
	{
		var isEmailValid = await _playerServiceClient
		.CheckPlayerInfoEmailAsync(command.Email, cancellationToken)
		.ConfigureAwait(false);

		if (isEmailValid == false)
			return new SetEmailResponse(StepStatus.EmailInvalid);

		var emailPlayer = await _playerServiceClient
			.GetPlayerInfoByEmailAsync(command.BrandId, command.Email, cancellationToken)
			.ConfigureAwait(false);

		if (emailPlayer != null)
			return new SetEmailResponse(StepStatus.EmailExisted);

		var updatedOn = DateTimeOffset.UtcNow;
		await _playerOperations
			.SetEmailAsync(command.PlayerCode, command.Email, updatedOn)
			.ConfigureAwait(false);
		return new SetEmailResponse(StepStatus.Valid);
	}
}