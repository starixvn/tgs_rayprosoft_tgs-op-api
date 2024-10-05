namespace TGS.Services.Operator.ApplicationServices.Player;

public record VerifyPlayerSessionCommand(string PlayerCode, string? PlayerSession);

