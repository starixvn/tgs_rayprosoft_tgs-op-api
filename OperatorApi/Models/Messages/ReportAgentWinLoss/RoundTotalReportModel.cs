namespace TGS.Apis.OperatorApi.Models.Messages.ReportAgentWinLoss;

public record RoundTotalReportModel
{
    public decimal BetAmount { get; init; }

    public decimal ValidBetAmount { get; init; }

    public decimal WinLoss { get; init; }

    public decimal SettleAmount { get; init; }
}