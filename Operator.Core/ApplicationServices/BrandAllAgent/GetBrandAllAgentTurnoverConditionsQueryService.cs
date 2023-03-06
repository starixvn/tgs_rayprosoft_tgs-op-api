using System.Collections.Generic;
using System.Threading;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.BrandAllAgent;

internal class GetBrandAllAgentTurnoverConditionsQueryService : IQueryService<GetBrandAllAgentTurnoverConditionsQuery, BrandAllAgentTurnover>
{
	private readonly IBrandRepository _brandRepository;

	public GetBrandAllAgentTurnoverConditionsQueryService(IBrandRepository brandRepository)
	{
		_brandRepository = brandRepository ?? throw new System.ArgumentNullException(nameof(brandRepository));
	}

	public IAsyncEnumerable<BrandAllAgentTurnover> QueryAsync(
		GetBrandAllAgentTurnoverConditionsQuery query,
		CancellationToken cancellationToken = default)
		=> _brandRepository.GetBrandAllAgentTurnoverConditionsAsync(query.BrandId, cancellationToken);
}
