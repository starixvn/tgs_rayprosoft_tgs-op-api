using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;
using TGS.Core.Infrastructures.Redis;
using TGS.Core.Models.Enums;

namespace TGS.Services.Operator.ApplicationServices.Player;
public class VerifyPlayerSessionCommandService(IRedisRepository repository) : IGetService<VerifyPlayerSessionCommand, bool>
{
	public async ValueTask<bool> GetAsync(VerifyPlayerSessionCommand query, CancellationToken cancellationToken = default)
	{
		if (query.PlayerSession is null)
		{
			return false;
		}

		var key = $"{RedisCacheType.Player}-{query.PlayerCode}-session";
		var db = repository.GetDb();
		var existingKey = await db.KeyExistsAsync(key);

		if (existingKey)
		{
			var value = db.StringGet(key);
			return !string.IsNullOrEmpty(value) && value == query.PlayerSession;
		}

		return false;
	}
}
