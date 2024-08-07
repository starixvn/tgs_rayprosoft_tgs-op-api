using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Tgs.BrandCertificate.Clients;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Brand;

internal class BrandPlayerContractDisplayQueryService(IBrandServiceClient brandServiceClient)
	: IQueryService<BrandPlayerContractDisplayQuery, BrandPlayerContractDisplayInfo>
{
	public async IAsyncEnumerable<BrandPlayerContractDisplayInfo> QueryAsync(
		BrandPlayerContractDisplayQuery query,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var displayInfos = await brandServiceClient
			.ListBrandPlayerContractDisplayAsync(query.BrandId, cancellationToken)
			.Where(b => b.IsVisualized == true)
			.ToArrayAsync(cancellationToken);

		foreach (var item in displayInfos)
		{
			yield return new BrandPlayerContractDisplayInfo(item.Name, item.IsVisualized);
		}
	}
}