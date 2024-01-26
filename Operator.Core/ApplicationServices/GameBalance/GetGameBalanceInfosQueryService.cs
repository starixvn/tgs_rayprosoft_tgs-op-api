using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Tgs.ServiceUtility;
using TGS.Core.DataServices.Base.Extensions;
using TGS.Core.DataServices.Tgs;
using TGS.Core.Models.Enums;

//using System.Linq.Dynamic;

namespace TGS.Services.Operator.ApplicationServices.GameBalance;

internal class GetGameBalanceInfosQueryService(ITgsRepository tgsRepository) : IQueryService<GetGameBalanceInfosQuery, GameBalanceRecord>
{
	private readonly ITgsRepository _tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));

	public async IAsyncEnumerable<GameBalanceRecord> QueryAsync(GetGameBalanceInfosQuery query,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var brand = await _tgsRepository.Brands.FirstAsync(
			x => x.Id == query.BrandId,
			cancellationToken: cancellationToken).ConfigureAwait(false);
		var brandName = brand.Name;

		var records = from trans in _tgsRepository.Transactions.AsQueryable()
					  where trans.BrandId == query.BrandId
						&& trans.PlayerCode == query.PlayerCode
						&& trans.Type >= TransactionType.GameProviderFundOut
						&& trans.Type <= TransactionType.GameProviderFundIn
					  join gpftt in _tgsRepository.GameProviderFundTransferTransactions on trans.Id equals gpftt.TransactionId
					  join gp in _tgsRepository.GameProviders on gpftt.GameProviderId equals gp.Id
					  select new GameBalanceRecord()
					  {
						  Amount = trans.Amount,
						  From = trans.Type == TransactionType.GameProviderFundIn ? gp.Name : brandName,
						  To = trans.Type == TransactionType.GameProviderFundIn ? brandName : gp.Name,
						  BalanceTime = trans.RecordedOn
					  };

		if (query.RecordOnRange != null)
		{
			records = records.Where(x => query.RecordOnRange.Value.Begin <= x.BalanceTime
				&& x.BalanceTime < query.RecordOnRange.Value.End);
		}

		foreach (var item in records)
		{
			yield return item;
		}
	}
}
