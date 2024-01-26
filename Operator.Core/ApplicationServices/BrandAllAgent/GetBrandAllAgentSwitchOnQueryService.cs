using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.BrandAllAgent;

internal class GetBrandAllAgentSwitchOnQueryService(IBrandRepository brandRepository) : IGetService<GetBrandAllAgentSwitchOnQuery, bool>
{
	private readonly IBrandRepository _brandRepository = brandRepository ?? throw new System.ArgumentNullException(nameof(brandRepository));

	public ValueTask<bool> GetAsync(
		GetBrandAllAgentSwitchOnQuery query,
		CancellationToken cancellationToken = default)
		=> _brandRepository.CheckHaveAllAgentAsync(query.BrandId, cancellationToken);
}
