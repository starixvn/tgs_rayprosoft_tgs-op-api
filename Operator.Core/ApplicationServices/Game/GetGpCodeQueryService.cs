using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;
using TGS.Services.Operator.GameProviders;

namespace TGS.Services.Operator.ApplicationServices.Game;

internal class GetGpCodeQueryService(
	IGameProviderRepository gameProviderRepository) : IGetService<GetGpCodeQuery, BrandSubGameProviderInfo>
{
	private readonly IGameProviderRepository _gameProviderRepository = gameProviderRepository ?? throw new ArgumentNullException(nameof(gameProviderRepository));

	public async ValueTask<BrandSubGameProviderInfo> GetAsync(
		GetGpCodeQuery query,
		CancellationToken cancellationToken = default)
	{
		var subGp = await _gameProviderRepository
			.QueryAllSubGameProvidersAsync(query.BrandId)
			.Where(bgp => bgp.Code == query.SubGpCode)
			.FirstOrDefaultAsync(cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		return subGp ?? throw new ArgumentException(
			$"No game provider of code '{query.SubGpCode}' is found");
	}
}