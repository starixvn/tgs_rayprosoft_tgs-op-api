namespace TGS.Apis.OperatorApi.Models.Messages;

public record VarifyForgotPasswordCodeRequest(
    string Token,
    string ValidateCode);
