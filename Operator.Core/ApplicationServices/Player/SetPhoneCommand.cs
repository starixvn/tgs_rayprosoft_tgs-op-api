namespace TGS.Services.Operator.ApplicationServices.Player;

public record SetPhoneCommand(string PlayerCode, string AreaCode, string Phone);