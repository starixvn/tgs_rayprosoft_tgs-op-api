using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;
using TGS.Core.DataServices.Tgs;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class CheckBrandPhoneExistsQueryService(IPlayerServiceClient playerServiceClient) : IGetService<CheckBrandPhoneExistsQuery, bool>
{
	public async ValueTask<bool> GetAsync(CheckBrandPhoneExistsQuery query, CancellationToken cancellationToken = default)
	{
		var player = await playerServiceClient.GetPlayerInfoByPhoneAsync(
			query.BrandId,
			query.AreaCode,
			query.Phone,
			cancellationToken).ConfigureAwait(false);

		return player is null;
	}
}