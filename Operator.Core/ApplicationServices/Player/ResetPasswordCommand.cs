namespace TGS.Services.Operator.ApplicationServices.Player;

public record ResetPasswordCommand(string SecurityKey, string ValidateCode, string NewPassword);