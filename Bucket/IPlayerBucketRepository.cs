using System.Threading.Tasks;
using TGS.Services.PlayerBucket.Models;

namespace TGS.Services.PlayerBucket;

public interface IPlayerBucketRepository
{
    ValueTask<CreatePlayerBucketWithDepositResponse> CreatePlayerBucketWithDepositAsync(
        CreatePlayerBucketWithDepositRequest request);

    Task CreatePlayerBucketsAsync(CreatePlayerBucketRequest[] request);
    Task FillInBucketAsync(FillInPlayerBucketsRequest request);
    ValueTask<bool> IsBucketFullAsync(string playerCode);
    ValueTask<decimal> ClearPlayerBucketsAsync(string playerCode);
}