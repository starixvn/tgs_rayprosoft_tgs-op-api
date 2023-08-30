using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tgs.PlayerBucket.Client;
using Tgs.Service.PlayerBuckets;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Withdraw;

internal class GetRequireBucketAmountQueryService
	: IGetService<GetRequireBucketAmountQuery, decimal>
	, IQueryService<GetRequireBucketAmountQuery, TypeBucketResult>
{
	private readonly IPlayerBucketClient _playerBucketClient;
	private readonly IPlayerBucketForTypesClient _playerBucketForTypesClient;

	public GetRequireBucketAmountQueryService(IPlayerBucketClient playerBucketClient, IPlayerBucketForTypesClient playerBucketForTypesClient)
	{
		_playerBucketClient = playerBucketClient ?? throw new ArgumentNullException(nameof(playerBucketClient));
		_playerBucketForTypesClient = playerBucketForTypesClient ?? throw new ArgumentNullException(nameof(playerBucketForTypesClient));
	}

	public ValueTask<decimal> GetAsync(GetRequireBucketAmountQuery query, CancellationToken cancellationToken = default)
		=> QueryAsync(query, cancellationToken)
			.Select(result => result.Result.Amount)
			.DefaultIfEmpty()
			.MaxAsync(cancellationToken);

	public IAsyncEnumerable<TypeBucketResult> QueryAsync(GetRequireBucketAmountQuery query, CancellationToken cancellationToken = default)
		=> _playerBucketClient
			.GetBrandBucketTypesAsync(query.BrandId, cancellationToken)
			.ToArrayAsync(cancellationToken)
			.AsTask()
			.ToAsyncEnumerable()
			.SelectMany(types => _playerBucketForTypesClient
				.ReadBucketsForBucketTypesAsync(query.PlayerCode, types, cancellationToken));
}
