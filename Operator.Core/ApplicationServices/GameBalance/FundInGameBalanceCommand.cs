namespace TGS.Services.Operator.ApplicationServices.GameBalance;

public record FundInGameBalanceCommand(string GameProviderCode, string PlayerCode, int Amount);