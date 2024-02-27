using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Tgs.ServiceUtility;
using TGS.Core.DataServices.Tgs;

namespace TGS.Services.Operator.ApplicationServices.BrandLanguage;

internal class GetBrandLanguageQueryService(ITgsRepository tgsRepository) : IQueryService<BrandLanguageQuery, BrandLanguageInfo>
{
	private readonly ITgsRepository _tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));

	public async IAsyncEnumerable<BrandLanguageInfo> QueryAsync(
		BrandLanguageQuery query,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var brand = await _tgsRepository.Brands.FirstAsync(x => x.Id == query.BrandId, cancellationToken: cancellationToken);

		foreach (var language in brand.Languages)
		{
			yield return new BrandLanguageInfo()
			{
				CultureCode = language.CultureCode
			};
		}
	}
}