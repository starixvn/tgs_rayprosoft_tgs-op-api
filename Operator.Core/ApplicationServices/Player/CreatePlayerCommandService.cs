using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.BrandCertificate.Clients;
using Tgs.Core.Infrastructures.Security;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;
using TGS.Services.Operations;
using TGS.Services.Operations.Models.Services;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class CreatePlayerCommandService : IGetService<CreatePlayerCommand, CreatePlayerInfo>
{
	private readonly IBrandOperations _brandOperations;
	private readonly IBrandServiceClient _brandServiceClient;
	private readonly IPasswordHandler _passwordHandler;
	private readonly IPlayerOperations _playerOperations;
	private readonly IPlayerServiceClient _playerServiceClient;

	public CreatePlayerCommandService(
		IPlayerOperations playerOperations,
		IPasswordHandler passwordHandler,
		IBrandOperations brandOperations,
		IPlayerServiceClient playerServiceClient,
		IBrandServiceClient brandServiceClient)
	{
		_playerOperations = playerOperations ?? throw new ArgumentNullException(nameof(playerOperations));
		_passwordHandler = passwordHandler ?? throw new ArgumentNullException(nameof(passwordHandler));
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

		var (salt, password) = _passwordHandler.GenerateCredential(command.Password);

		_ = await _brandServiceClient
			.GetBrandInfoAsync(command.BrandId, cancellationToken)
			.ConfigureAwait(false) ?? throw new BrandNotFoundException();

		var betLimitId = await _brandOperations
			.GetBrandDefaultBetLimitAsync(command.BrandId, command.CurrencyCode)
			.ConfigureAwait(false);

		var resultPlayerCode = await CreatePlayerAsync(
			command,
			salt!,
			password!,
			command.CurrencyCode,
			betLimitId,
			PlayerStatus.RegisterUndone).ConfigureAwait(false);

		return new CreatePlayerInfo(resultPlayerCode, true);
	}

	private Task<string> CreatePlayerAsync(
		CreatePlayerCommand command,
		byte[] salt,
		byte[] password,
		string currency,
		int betLimitId,
		PlayerStatus status)
		=> _playerOperations.CreatePlayerAsync(
			new CreatePlayerData
			{
				BrandId = command.BrandId,
				UserName = command.UserName,
				Salt = salt,
				Password = password,
				CurrencyCode = currency,
				BetLimitId = betLimitId,
				Type = command.IsTestPlayer
					? Core.Models.Enums.PlayerType.TestPlayer
					: Core.Models.Enums.PlayerType.RealPlayer,
				PlayerStatus = (Core.Models.Enums.PlayerStatus)status
			});
}
