using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TGS.Core.DataServices.Base.Extensions;
using TGS.Core.DataServices.Tgs;

namespace TGS.Services.Operations.Queries;

public interface IWalletQueries
{
	Task<decimal> GetBalanceAsync(Guid walletId);
}

public class WalletQueries(ITgsRepository repository) : IWalletQueries
{
	Task<decimal> IWalletQueries.GetBalanceAsync(Guid walletId)
	{
		const string spName = "dbo.wallet_get_balance";
		return repository.SqlQuerySingleAsync<decimal>(spName, parameters: new Dictionary<string, object>
		{
			{":wallet_id", walletId}
		});
	}
}