namespace TGS.Services.Operator.ApplicationServices.GameBalance;

public record FundOutGameBalanceCommand(string GameProviderCode, string PlayerCode, int Amount);