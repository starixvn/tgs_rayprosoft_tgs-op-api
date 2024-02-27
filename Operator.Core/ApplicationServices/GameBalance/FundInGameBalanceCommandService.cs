using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tgs.GameProviders.Pluggable.Client;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.GameBalance;

internal class FundInGameBalanceCommandService(
	IGameProvidersOperatorPluggableClient gameProvidersOperatorPluggableClient,
	ILogger<FundInGameBalanceCommandService> logger) : ICommandService<FundInGameBalanceCommand>
{
	private readonly IGameProvidersOperatorPluggableClient _gameProvidersOperatorPluggableClient = gameProvidersOperatorPluggableClient ?? throw new ArgumentNullException(nameof(gameProvidersOperatorPluggableClient));
	private readonly ILogger<FundInGameBalanceCommandService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

	public async ValueTask ExecuteAsync(FundInGameBalanceCommand command, CancellationToken cancellationToken = default)
	{
		await _gameProvidersOperatorPluggableClient.GameProviderFundInAsync(
		  playerCode: command.PlayerCode,
		  gameProviderCode: command.GameProviderCode,
		  amount: command.Amount,
		  cancellationToken: cancellationToken).ConfigureAwait(false);

		_logger.LogInformation("The game wallet was successfully transferred to the main wallet.");
	}
}