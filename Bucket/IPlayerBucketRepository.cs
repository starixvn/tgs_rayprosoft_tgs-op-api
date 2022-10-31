using System.Threading.Tasks;
using TGS.Services.PlayerBucket.Models;

namespace TGS.Services.PlayerBucket;

public interface IPlayerBucketRepository
{
	ValueTask<decimal> ClearPlayerBucketsAsync(string playerCode);

	Task CreatePlayerBucketsAsync(CreatePlayerBucketRequest[] request);

	ValueTask<CreatePlayerBucketWithDepositResponse> CreatePlayerBucketWithDepositAsync(
				CreatePlayerBucketWithDepositRequest request);

	Task FillInBucketAsync(FillInPlayerBucketsRequest request);

	ValueTask<bool> IsBucketFullAsync(string playerCode);
}
