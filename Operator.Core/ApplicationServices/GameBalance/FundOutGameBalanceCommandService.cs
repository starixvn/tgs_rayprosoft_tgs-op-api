using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tgs.GameProviders.Pluggable.Client;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.GameBalance;

internal class FundOutGameBalanceCommandService(
	IGameProvidersOperatorPluggableClient gameProvidersOperatorPluggableClient,
	ILogger<FundOutGameBalanceCommandService> logger) : ICommandService<FundOutGameBalanceCommand>
{
	private readonly IGameProvidersOperatorPluggableClient _gameProvidersOperatorPluggableClient = gameProvidersOperatorPluggableClient ?? throw new ArgumentNullException(nameof(gameProvidersOperatorPluggableClient));
	private readonly ILogger<FundOutGameBalanceCommandService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

	public async ValueTask ExecuteAsync(FundOutGameBalanceCommand command, CancellationToken cancellationToken = default)
	{
		await _gameProvidersOperatorPluggableClient.GameProviderFundOutAsync(
		   playerCode: command.PlayerCode,
		   gameProviderCode: command.GameProviderCode,
		   amount: command.Amount,
		   cancellationToken: cancellationToken).ConfigureAwait(false);

		_logger.LogInformation("The main wallet was successfully transferred to the game manufacturer.");
	}
}
