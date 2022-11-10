using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tgs.Service.PlayerBuckets;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Withdraw;

internal class GetRequireBucketAmountQueryService : IGetService<GetRequireBucketAmountQuery, decimal>
{
	private readonly IPlayerBucketClient _playerBucketClient;

	public GetRequireBucketAmountQueryService(IPlayerBucketClient playerBucketClient)
	{
		_playerBucketClient = playerBucketClient ?? throw new ArgumentNullException(nameof(playerBucketClient));
	}

	public ValueTask<decimal> GetAsync(GetRequireBucketAmountQuery query, CancellationToken cancellationToken = default)
		=> _playerBucketClient.ReadBucketsAsync(query.PlayerCode, cancellationToken)
			.AsTask()
			.ToAsyncEnumerable()
			.Select(result => result.Amount)
			.SingleAsync(cancellationToken: cancellationToken);
}
