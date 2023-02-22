using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.BrandAllAgent;

internal class GetBrandAllAgentQueryService : IGetService<GetBrandAllAgentQuery, bool>
{
	private readonly IBrandRepository _brandRepository;

	public GetBrandAllAgentQueryService(IBrandRepository brandRepository)
	{
		_brandRepository = brandRepository ?? throw new System.ArgumentNullException(nameof(brandRepository));
	}

	public ValueTask<bool> GetAsync(
		GetBrandAllAgentQuery query,
		CancellationToken cancellationToken = default)
		=> _brandRepository.CheckHaveAllAgentAsync(query.BrandId, cancellationToken);
}
