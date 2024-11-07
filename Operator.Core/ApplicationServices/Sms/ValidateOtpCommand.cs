namespace TGS.Services.Operator.ApplicationServices.Sms;

public record ValidateOtpCommand(string ValidateCode, string SecurityKey);