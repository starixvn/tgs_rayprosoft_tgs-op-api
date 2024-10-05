namespace TGS.Services.Operator.ApplicationServices.Player;

public record CreateDeletePlayerSessionCommand(string PlayerCode, bool IsCreate);
