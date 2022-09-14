namespace TGS.Services.Operator;
public record struct PlayerLocationInfo(
    string? PlayerCode,
    string Ip,
    string? HostName,
    string? AgentInvitationCode);
