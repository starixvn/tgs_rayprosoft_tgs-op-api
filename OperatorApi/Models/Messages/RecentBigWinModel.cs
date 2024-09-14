namespace TGS.Apis.OperatorApi.Models.Messages;
using TGS.Core.Models.Enums;

public record RecentBigWinModel()
{
	public int GameId { get; init; }
	public string GameName { get; init; }
	public string GameCode { get; init; }
	public GameType GameType { get; init; }
	public string GameProviderCode { get; init; }
	public DateTime CreateTime { get; init; }
	public string CurrencyName { get; init; }
	public string PlayerName { get; init; }
	public string PlayerCode { get; init; }
	public int PlayerVipLevel { get; init; }
	public double WinAmount { get; init; }
}
