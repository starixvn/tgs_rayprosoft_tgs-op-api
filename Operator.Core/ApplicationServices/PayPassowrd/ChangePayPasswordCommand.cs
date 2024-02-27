namespace TGS.Services.Operator.ApplicationServices.PayPassowrd;

public record ChangePayPasswordCommand(
	string PlayerCode,
	string NewPassword,
	string ValidateCode,
	string SecurityKey);