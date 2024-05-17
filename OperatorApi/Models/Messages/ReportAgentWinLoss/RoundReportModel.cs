using TGS.Core.Models.Enums;

namespace TGS.Apis.OperatorApi.Models.Messages.ReportAgentWinLoss;

public record RoundReportModel
{
    public Guid Id { get; init; }

    public string ExternalRoundId { get; init; }

    public string GameName { get; init; }

    public string PlayerName { get; init; }

    public string PlayerCode { get; init; }

    public string GameProviderName { get; init; }

    public string SubGameProviderName { get; init; }

    public ClientRoundStatus Status { get; init; }

    public DateTimeOffset CreatedOn { get; init; }

    public DateTimeOffset? ClosedOn { get; init; }

    public decimal? BetAmount { get; init; }

    public decimal? ValidBetAmount { get; init; }

    public decimal? Turnover { get; init; }

    public decimal? SettleAmount { get; init; }

    public decimal? WinLoss { get; init; }
}