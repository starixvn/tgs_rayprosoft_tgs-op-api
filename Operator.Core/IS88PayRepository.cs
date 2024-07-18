using System;
using System.Threading;
using System.Threading.Tasks;

namespace TGS.Services.Operator;

public interface IS88PayRepository
{
	ValueTask<string?> GetDepositAsync(Guid tokenId, CancellationToken cancellationToken = default);

	ValueTask<bool> StoreDepositAsync(Guid tokenId, string json, CancellationToken cancellationToken = default);
}