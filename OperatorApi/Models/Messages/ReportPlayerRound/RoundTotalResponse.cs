namespace TGS.Apis.OperatorApi.Models.Messages.ReportPlayerRound;

public record RoundTotalResponse
{
    public decimal BetAmount { get; init; }

    public decimal ValidBetAmount { get; init; }

    public decimal WinLoss { get; init; }

    public decimal SettleAmount { get; init; }
}