namespace TGS.Apis.OperatorApi.Models.Messages.ReportAgentContribution;

public record AgentContributionTotalModel(
    decimal DepositAmount,
    decimal DepositHandlingFee,
    decimal WithdrawalAmount,
    decimal CompanyAdjustAmount,
    decimal BonusAdjustAmount,
    decimal AgentAdjustAmount,
    decimal BonusAmount,
    decimal BetAmount,
    decimal ValidBetAmount,
    decimal Turnover,
    decimal WinLossAmount,
    decimal NetProfit);