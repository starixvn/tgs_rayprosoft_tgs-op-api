using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.GameProviders.Pluggable.Client;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.GameBalance;

internal class GetGameBalanceQueryService(IGameProvidersOperatorPluggableClient providersOperatorPluggableClient) : IGetService<GetGameBalanceQuery, decimal>
{
	private readonly IGameProvidersOperatorPluggableClient _providersOperatorPluggableClient = providersOperatorPluggableClient ?? throw new ArgumentNullException(nameof(providersOperatorPluggableClient));

	public ValueTask<decimal> GetAsync(GetGameBalanceQuery query, CancellationToken cancellationToken = default)
		=> _providersOperatorPluggableClient.GetBalanceAsync(
			playerCode: query.PlayerCode,
			gameProviderCode: query.GameProviderCode,
			cancellationToken: cancellationToken);
}
