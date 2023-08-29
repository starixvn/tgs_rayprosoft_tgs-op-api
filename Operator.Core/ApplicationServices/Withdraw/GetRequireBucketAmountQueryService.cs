using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tgs.PlayerBucket.Client;
using Tgs.Service.PlayerBuckets;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Withdraw;

internal class GetRequireBucketAmountQueryService : IGetService<GetRequireBucketAmountQuery, decimal>
{
	private readonly IPlayerBucketClient _playerBucketClient;
	private readonly IPlayerBucketForTypesClient _playerBucketForTypesClient;

	public GetRequireBucketAmountQueryService(IPlayerBucketClient playerBucketClient, IPlayerBucketForTypesClient playerBucketForTypesClient)
	{
		_playerBucketClient = playerBucketClient ?? throw new ArgumentNullException(nameof(playerBucketClient));
		_playerBucketForTypesClient = playerBucketForTypesClient ?? throw new ArgumentNullException(nameof(playerBucketForTypesClient));
	}

	public async ValueTask<decimal> GetAsync(GetRequireBucketAmountQuery query, CancellationToken cancellationToken = default)
	{
		var bucketTypes = await _playerBucketClient
			.GetBrandBucketTypesAsync(query.BrandId, cancellationToken)
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		return await _playerBucketForTypesClient
			.ReadBucketsForBucketTypesAsync(query.PlayerCode, bucketTypes, cancellationToken)
			.Select(result => result.Result.Amount)
			.DefaultIfEmpty()
			.MaxAsync(cancellationToken)
			.ConfigureAwait(false);
	}
}
