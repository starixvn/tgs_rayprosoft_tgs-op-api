using System;

namespace TGS.Services.Operator;

public record RecentBigWinModel
{
	public int GameId { get; init; }
	public string GameName { get; init; }
	public string GameCode { get; init; }
	public string GameProviderCode { get; init; }
	public string GameProviderName { get; init; }
	public DateTime CreateTime { get; init; }
	public string CurrencyCode { get; init; }
	public string PlayerName { get; init; }
	public string PlayerCode { get; init; }
	public Guid BetId { get; init; }
	public double BetAmount { get; init; }
	public double Profit { get; init; }
	public double Payout { get; init; }
}
