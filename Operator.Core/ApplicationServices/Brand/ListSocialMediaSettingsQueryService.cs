using System.Collections.Generic;
using System.Threading;
using Tgs.BrandCertificate.Clients;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Brand;

internal class ListSocialMediaSettingsQueryService(IBrandServiceClient brandServiceClient)
	: IQueryService<ListSocialMediaSettingsQuery, SocialMediaSetting>
{
	public IAsyncEnumerable<SocialMediaSetting> QueryAsync(ListSocialMediaSettingsQuery query, CancellationToken cancellationToken = default)
		=> brandServiceClient.ListSocialMediaSettingsAsync(query.BrandId, cancellationToken);
}