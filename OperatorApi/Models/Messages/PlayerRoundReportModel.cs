namespace TGS.Apis.OperatorApi.Models.Messages;

public record PlayerRoundReportModel
{
	public Guid Id { get; set; }
	public DateTimeOffset CreatedOn { get; set; }
	public string GameProviderName { get; set; }
	public decimal? BetAmount { get; set; }
	public decimal? ValidBetAmount { get; set; }
	public decimal? WinLoss { get; set; }
}