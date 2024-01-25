using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tgs.Paging;
using TGS.Core.DataServices.Tgs;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Vip;

namespace TGS.Operator.Repositories;

internal class VipLevelRepository(ITgsRepository tgsRepository) : IVipLevelRepository
{
	private readonly ITgsRepository _tgsRepository = tgsRepository ??
			throw new ArgumentNullException(nameof(tgsRepository));

	public async ValueTask<int?> GetVipLevelAsync(Guid vipLevelId)
		=> await _tgsRepository.VipLevels.AsQueryable()
			.Where(vip => vip.Id == vipLevelId)
			.Select(vip => (int?)vip.Level)
			.FirstOrDefaultAsync();

	public IAsyncEnumerable<VipRecord> QueryVipLevelsAsync(Guid brandId, Paging<VipRecord> paging)
	{
		var query = QueryVipLevelsAsync(brandId)
			.ApplyOrderBy(paging)
			.ApplyPaging(paging);

		return query;
	}

	public IAsyncEnumerable<VipRecord> QueryVipLevelsAsync(Guid brandId)
	{
		var query = from vip in _tgsRepository.VipLevels.AsQueryable()
					.AsAsyncEnumerable()
					where vip.BrandId == brandId
					select new VipRecord
					{
						Level = vip.Level,
						LevelUpAccumulateBetAmount = vip.LevelUpAccumulateBetAmount,
						LevelUpAccumulateDeposit = vip.LevelUpAccumulateDeposit,
						LevelKeepAccumulateBetAmount = vip.LevelKeepAccumulateBetAmount,
						LevelKeepAccumulateDeposit = vip.LevelKeepAccumulateDeposit,
						BirthBenefit = vip.BirthReward,
						PromotionBenefit = vip.PromotionReward,
						QuarterlyBenefit = vip.QuarterlyReward,
						LiveDealerRebate = vip.LiveDealerRebatePercentage,
						SlotRebate = vip.SlotRebatePercentage,
						CardGameRebate = vip.CardGameRebatePercentage,
						SportsRebate = vip.SportsRebatePercentage,
						VirtualSportsRebate = vip.VirtualSportsRebatePercentage,
						LotteryRebate = vip.LotteryRebatePercentage
					};

		return query;
	}

	public ValueTask<int> QueryVipLevelsTotalCountAsync(Guid brandId)
		=> QueryVipLevelsAsync(brandId).CountAsync();
}
