using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Tgs.ServiceUtility;
using TGS.Core.DataServices.Tgs;

namespace TGS.Services.Operator.ApplicationServices.BrandCommunication;

internal class GetBrandCommunicationQueryService : IQueryService<BrandCommunicationQuery, BrandCommunicationInfo>
{
	private readonly ITgsRepository _tgsRepository;

	public GetBrandCommunicationQueryService(ITgsRepository tgsRepository)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
	}

	public IAsyncEnumerable<BrandCommunicationInfo> QueryAsync(
		BrandCommunicationQuery query,
		CancellationToken cancellationToken = default)
	{
		var result = _tgsRepository.BrandCommunications
			.AsAsyncEnumerable()
			.Where(x => x.BrandId == query.BrandId);

		return result.Select(x => new BrandCommunicationInfo()
		{
			CommunicationType = x.Type,
			Phone = x.Phone,
			AppId = x.AppId,
			QRCodeUrl = x.QRCodeUrl
		});
	}
}
