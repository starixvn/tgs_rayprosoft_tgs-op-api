using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;
using TGS.Core.Infrastructures.Redis;
using TGS.Core.Models.Enums;

namespace TGS.Services.Operator.ApplicationServices.Player
{
	public class CreateDeletePlayerSessionCommandService(IRedisRepository repository) : IGetService<CreateDeletePlayerSessionCommand, Guid>
	{
		public async ValueTask<Guid> GetAsync(CreateDeletePlayerSessionCommand query, CancellationToken cancellationToken = default)
		{
			var key = $"{RedisCacheType.Player}-{query.PlayerCode}-session";
			if (query.IsCreate)
			{
				var session = Guid.NewGuid();

				await CacheSessionAsync(key, session.ToString(), TimeSpan.FromHours(24));

				return session;
			}

			await ClearSessionAsync(key);

			return Guid.Empty;
		}

		private async Task CacheSessionAsync(string key, object value, TimeSpan expiresIn)
		{
			var db = repository.GetDb();
			await db.StringSetAsync(key, value.ToString(), expiresIn);
		}

		private async Task ClearSessionAsync(string key)
		{
			var db = repository.GetDb();
			await db.KeyDeleteAsync(key);
		}
	}
}