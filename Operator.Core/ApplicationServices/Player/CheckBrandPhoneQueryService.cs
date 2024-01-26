using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tgs.ServiceUtility;
using TGS.Core.DataServices.Tgs;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class CheckBrandPhoneQueryService(ITgsRepository tgsRepository) : IGetService<CheckBrandPhoneQuery, bool>
{
	private readonly ITgsRepository _tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));

	public async ValueTask<bool> GetAsync(CheckBrandPhoneQuery query, CancellationToken cancellationToken = default)
	{
		var player = await _tgsRepository.Players.AsQueryable()
			.Where(p => p.BrandId == query.BrandId
				&& p.AreaCode == query.AreaCode
				&& p.Phone == query.Phone)
			.FirstOrDefaultAsync(cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		return player is null || query.PlayerCode is not null && player.PlayerCode == query.PlayerCode;
	}
}
