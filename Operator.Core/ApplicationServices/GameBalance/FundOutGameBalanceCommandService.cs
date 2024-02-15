using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tgs.GameProviders.Pluggable.Client;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.GameBalance;

internal class FundOutGameBalanceCommandService(
	IGameProvidersOperatorPluggableClient gameProvidersOperatorPluggableClient,
	ILogger<FundOutGameBalanceCommandService> logger)
	: ICommandService<FundOutGameBalanceCommand>
{
	public async ValueTask ExecuteAsync(FundOutGameBalanceCommand command, CancellationToken cancellationToken = default)
	{
		if (command.Amount <= 0)
			return;

		await gameProvidersOperatorPluggableClient.GameProviderFundOutAsync(
		   playerCode: command.PlayerCode,
		   gameProviderCode: command.GameProviderCode,
		   amount: command.Amount,
		   cancellationToken: cancellationToken).ConfigureAwait(false);

		logger.LogInformation("The main wallet was successfully transferred to the game manufacturer.");
	}
}
