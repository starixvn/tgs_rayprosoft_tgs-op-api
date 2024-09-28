using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Tgs.ServiceUtility;
using TGS.Core.DataServices.Tgs;
using TGS.Services.Operator.ApplicationServices.MemoryCache;
using TGS.Services.Operator.GameProviders;

namespace TGS.Services.Operator.ApplicationServices.Game;
public class GetRecentBigWinQueryService(IGamesRepository categoryRepository, IMemoryCacheService memoryCache, ITgsRepository repository)
	: IGetService<GetRecentBigWinQuery, IEnumerable<RecentBigWinModel>>
{
	private readonly IGamesRepository _categoryRepository =
		categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
	private readonly ITgsRepository _repository =
		repository ?? throw new ArgumentNullException(nameof(repository));
	private readonly IMemoryCacheService _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));

	public async ValueTask<IEnumerable<RecentBigWinModel>> GetAsync(GetRecentBigWinQuery query, CancellationToken cancellationToken = default)
	{
		var hashKey = HashCode.Combine(query.BrandId, query.Top);
		var categories = await _memoryCache.GetOrCreateAsync(
			$"{query.BrandId}_GetGameType_{hashKey}",
			() => GetRecentBigWinsAsync(query, cancellationToken),
			new List<RecentBigWinModel>());

		return categories;
	}

	private async Task<IEnumerable<RecentBigWinModel>> GetRecentBigWinsAsync(GetRecentBigWinQuery query, CancellationToken cancellationToken = default)
	{
		var db = _repository.GetDbConnection();
		var parameters = new Dictionary<string, object>
		{
			{ "brandid", query.BrandId },
			{ "top", query.Top }
		};
		string sqlQuery = @"
                SELECT * FROM dbo.recent_big_win_get(
                    @brandid,
                    @top
                )";
		var recentReal = await db.QueryAsync<RecentBigWinModel>(sqlQuery, parameters);
		var recenBot = GenerateRandomGames(50);
		return recentReal.Concat(recenBot).OrderByDescending(x => x.Profit).Take(50);
	}

	public static IEnumerable<RecentBigWinModel> GenerateRandomGames(int count)
	{
		var games = new List<RecentBigWinModel>();
		var random = new Random();
		var playerNames = new[] { "Snickers", "Twitch", "Muffin", "Waffles", "Sprinkles", "Buttons", "Cupcake",
		"Noodle", "Whiskers", "Zigzag", "Flapjack", "Scooter", "Fudge", "Doodle", "Scrappy",
		"Bingo", "Chomper", "Gizmo", "Pudding", "Wobble", "Squiggle", "Fluffy", "Zoom",
		"Puff", "Tater", "Rumble", "Spud", "Pickles" };
		var gameBots = new GameInfo[]
		{
			new () { Id = 7400013, Code = "game-soccer", Name = "Soccer", GameProviderCode = "SABASW" , GameProviderName= "Saba Sports"},
			new () { Id = 7400002, Code = "game-basketball", Name = "Basketball", GameProviderCode = "SABASW" , GameProviderName= "Saba Sports"},
			new () { Id = 7400003, Code = "game-baseball", Name = "Baseball", GameProviderCode = "SABASW" , GameProviderName= "Saba Sports"},
			new () { Id = 7400015, Code = "game-badminton", Name = "Soccer", GameProviderCode = "SABASW" , GameProviderName= "Saba Sports"},
			new () { Id = 7400004, Code = "game-muaythai", Name = "Muay Thai", GameProviderCode = "SABASW" , GameProviderName= "Saba Sports"},
			new () { Id = 7900378, Code = "game-auto-roulette", Name = "Auto-Roulette", GameProviderCode = "SABAPLUS-PP" , GameProviderName= "Pragmatic Play"},
			new () { Id = 7900362, Code = "game-baccarat-1", Name = "Baccarat 1", GameProviderCode = "SABAPLUS-PP" , GameProviderName= "Pragmatic Play"},
			new () { Id = 7900363, Code = "game-baccarat-2", Name = "Baccarat 2", GameProviderCode = "SABAPLUS-PP" , GameProviderName= "Pragmatic Play"},
			new () { Id = 7900376, Code = "game-macao-roulette", Name = "Roulette Macao", GameProviderCode = "SABAPLUS-PP" , GameProviderName= "Pragmatic Play"},
			new () { Id = 7900367, Code = "game-speed-baccarat-10", Name = "Speed Baccarat 1", GameProviderCode = "SABAPLUS-PP" , GameProviderName= "Pragmatic Play"}
		};
		for (int i = 0; i < count; i++)
		{
			double betAmount = Math.Round(random.NextDouble() * 100, 2);
			double payout = Math.Round(betAmount + random.NextDouble() * 5000, 2);
			var indexGame = random.Next(gameBots.Length);
			var game = new RecentBigWinModel
			{
				GameId = gameBots[indexGame].Id,
				GameName = gameBots[indexGame].Name,
				GameCode = gameBots[indexGame].Code,
				GameProviderCode = gameBots[indexGame].GameProviderCode,
				GameProviderName = gameBots[indexGame].GameProviderName,
				CreateTime = DateTime.Now.AddMinutes(-random.Next(0, 1440)),
				CurrencyCode = "THB",
				PlayerName = playerNames[random.Next(playerNames.Length)],
				PlayerCode = $"PC{random.Next(1000, 9999)}",
				BetId = Guid.NewGuid(),
				BetAmount = betAmount,
				Payout = payout,
				Profit = Math.Round(payout - betAmount, 2)
			};
			games.Add(game);
		}
		return games;
	}
}