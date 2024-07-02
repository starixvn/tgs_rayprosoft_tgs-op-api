namespace TGS.Apis.OperatorApi.Models.Messages.ReportAgentContribution;

public record AgentContributionReportModel(
    string Username,
    short Type,
    decimal DepositAmount,
    decimal DepositHandlingFee,
    decimal WithdrawalAmount,
    decimal CompanyAdjustAmount,
    decimal AgentAdjustAmount,
    decimal BonusAdjustAmount,
    decimal BonusAmount,
    decimal BetAmount,
    decimal ValidBetAmount,
    decimal Turnover,
    decimal WinLossAmount,
    decimal NetProfit);