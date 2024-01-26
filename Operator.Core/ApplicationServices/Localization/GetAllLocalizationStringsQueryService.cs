using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Localization;

internal class GetAllLocalizationStringsQueryService(ISysRepository sysRepository)
		: IGetService<GetAllLocalizationStringsQuery, IDictionary<string, string>>
{
	private readonly ISysRepository _sysRepository = sysRepository ?? throw new ArgumentNullException(nameof(sysRepository));

	public async ValueTask<IDictionary<string, string>> GetAsync(
		GetAllLocalizationStringsQuery query,
		CancellationToken cancellationToken = default)
	{
		var allI18N = _sysRepository.QueryAllLocalizationStringsAsync(query.CultureCode);

		return await allI18N
			.ToDictionaryAsync(
				p => p.Key,
				p => p.Value,
				cancellationToken: cancellationToken)
			.ConfigureAwait(false);
	}
}
