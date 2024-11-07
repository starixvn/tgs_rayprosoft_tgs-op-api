namespace TGS.Apis.OperatorApi.Models.Messages;

public record RegisterByPhoneRequest(string Phone, string SecurityKey, string UserName, string Password, string AreaCode, string ValidateCode, string UpperPlayerCode, string AgentInvitationCode);