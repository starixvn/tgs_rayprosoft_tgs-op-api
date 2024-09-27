using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Tgs.BrandCertificate.Clients;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class CreateIdentityAuthenticationFormCommandService(
	IBrandServiceClient brandServiceClient,
	IPlayerRepository playerRepository) : ICommandService<CreateIdentityAuthenticationFormCommand>
{
	private readonly IBrandServiceClient _brandServiceClient = brandServiceClient ?? throw new ArgumentNullException(nameof(brandServiceClient));
	private readonly IPlayerRepository _playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));

	public async ValueTask ExecuteAsync(
		CreateIdentityAuthenticationFormCommand command,
		CancellationToken cancellationToken = default)
	{
		var brandInfo = await _brandServiceClient
			.GetBrandInfoAsync(command.BrandId, cancellationToken)
			.ConfigureAwait(false);

		//if (!brandInfo!.PlayerIdentityEnabled)
		//	throw new BrandEnablePlayerIdentityNotSetException();

		var data = JsonSerializer.Serialize(new PlayerAuthenticationFormData(command.FileId));

		var formId = await _playerRepository
			.CreatePlayerAuthenticationFormAsync(command.BrandId, command.PlayerCode, data)
			.ConfigureAwait(false);

		await _playerRepository
			.UpdatePlayerIdentityAsync(command.PlayerCode, formId)
			.ConfigureAwait(false);
	}
}