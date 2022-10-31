using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Tgs.CashService.Client;
using Tgs.MessageQueue;
using TGS.Core.DataServices.Tgs;
using TGS.Core.DataServices.Tgs.Models;
using TGS.Core.Models.Enums;
using TGS.Services.PlayerBucket;
using TGS.Services.PlayerBucket.Models;

namespace PlayerBucket.Repositories;

public class PlayerBucketRepository : IPlayerBucketRepository
{
	private readonly ICashServiceClient _cashServiceClient;
	private readonly IMessageSender _messageSender;
	private readonly ITgsRepository _repository;

	public PlayerBucketRepository(
		IMessageSender messageSender,
		ITgsRepository repository,
		ICashServiceClient cashServiceClient)
	{
		_messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
		_repository = repository ?? throw new ArgumentNullException(nameof(repository));
		_cashServiceClient = cashServiceClient ?? throw new ArgumentNullException(nameof(cashServiceClient));
	}

	public async ValueTask<decimal> ClearPlayerBucketsAsync(string playerCode)
	{
		await _repository.PlayerBuckets.Where(x => x.PlayerCode == playerCode)
			.ForEachAsync(x =>
			{
				x.CurrentBucketAmount = x.BucketAmount;
				x.IsFull = true;
			}).ConfigureAwait(false);
		_ = await _repository.SaveChangesAsync().ConfigureAwait(false);

		return await _repository.PlayerBuckets.Where(x => x.PlayerCode == playerCode && x.IsFull)
			.SumAsync(x => x.BucketAmount - x.CurrentBucketAmount);
	}

	public async Task CreatePlayerBucketsAsync(CreatePlayerBucketRequest[] requests)
	{
		var buckets = requests.Select(x => new TGS.Core.DataServices.Tgs.Models.PlayerBucket
		{
			PlayerCode = x.PlayerCode,
			BucketAmount = x.BucketAmount,
			BucketType = x.BucketType,
			IsFull = false,
			CreatedOn = DateTimeOffset.Now
		}).ToArray();

		await _repository.PlayerBuckets.AddRangeAsync(buckets).ConfigureAwait(false);
		_ = await _repository.SaveChangesAsync().ConfigureAwait(false);
	}

	public async ValueTask<CreatePlayerBucketWithDepositResponse> CreatePlayerBucketWithDepositAsync(
		CreatePlayerBucketWithDepositRequest request)
	{
		var depositBonusAmount = request.DepositAmount;
		var promotionPlayerId = default(int?);

		_ = await _cashServiceClient.DepositedAsync(
			request.DefaultWalletId,
			request.DepositAmount,
			request.Remark,
			request.FormId.ToString(),
			request.UserId).ConfigureAwait(false);

		if (request.PromotionId != null)
		{
			var promotion = await _repository.Promotions.FirstAsync(x => x.Id == request.PromotionId);

			var subject = $"tgs.promotion.{promotion.PromotionType.ToLower()}.distribute";

			var response = await _messageSender.RequestAsync(subject, Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request)))
				.ConfigureAwait(false);

			var json = Encoding.UTF8.GetString(response.Span);
			var responseData = JsonSerializer.Deserialize<DepositMessageDataResponse>(json)!;

			promotionPlayerId = responseData.PromotionBonusPlayerId;
			depositBonusAmount += responseData.DepositBonusAmount;
		}
		else
		{
			var bucket = new TGS.Core.DataServices.Tgs.Models.PlayerBucket
			{
				PlayerCode = request.PlayerCode,
				BucketAmount = request.DepositAmount,
				BucketType = BucketType.Deposit,
				IsFull = false,
				CreatedOn = DateTimeOffset.Now
			};

			_ = await _repository.PlayerBuckets.AddAsync(bucket).ConfigureAwait(false);

			_ = await _repository.SaveChangesAsync().ConfigureAwait(false);
		}

		return new CreatePlayerBucketWithDepositResponse(depositBonusAmount, promotionPlayerId);
	}

	public async Task FillInBucketAsync(FillInPlayerBucketsRequest request)
	{
		var playerCodes = request.PlayerTurnovers.Select(x => x.PlayerCode).ToArray();

		var playerBuckets = await _repository.PlayerBuckets
			.Where(x => playerCodes.Contains(x.PlayerCode) && !x.IsFull).ToArrayAsync();
		var playerBucketHistories = new List<PlayerBucketHistory>();

		foreach (var player in request.PlayerTurnovers)
		{
			var turnover = player.Turnover;
			var buckets = playerBuckets.Where(x => x.PlayerCode == player.PlayerCode).ToArray();

			foreach (var playerBucket in buckets)
			{
				if (turnover <= 0)
				{
					break;
				}

				var amount = 0.0M;

				if (turnover - (playerBucket.BucketAmount - playerBucket.CurrentBucketAmount) >= 0.0M)
				{
					amount = playerBucket.BucketAmount - playerBucket.CurrentBucketAmount;
					turnover -= amount;
					playerBucket.IsFull = true;
					playerBucket.CurrentBucketAmount = playerBucket.BucketAmount;
				}
				else
				{
					amount = turnover;
					playerBucket.CurrentBucketAmount += turnover;
					turnover = 0;
				}

				playerBucketHistories.Add(new()
				{
					PlayerCode = player.PlayerCode,
					Turnover = amount,
					From = request.From,
					To = request.To,
					CreatedOn = DateTimeOffset.Now
				});
			}
		}

		using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

		_repository.PlayerBuckets.UpdateRange(playerBuckets);
		_repository.PlayerBucketHistories.UpdateRange(playerBucketHistories);
		_ = await _repository.SaveChangesAsync().ConfigureAwait(false);

		scope.Complete();
	}

	public async ValueTask<bool> IsBucketFullAsync(string playerCode) =>
		await _repository.PlayerBuckets.Where(x => x.PlayerCode == playerCode).AllAsync(x => x.IsFull);
}
