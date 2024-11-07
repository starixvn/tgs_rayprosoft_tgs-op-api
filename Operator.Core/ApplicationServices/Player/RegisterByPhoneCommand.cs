using System;

namespace TGS.Services.Operator.ApplicationServices.Player;

public record RegisterByPhoneCommand(
	string UserName,
	string Password,
	string AreaCode,
	string Phone,
	string AgentInvitationCode,
	string UpperPlayerCode,
	Guid BrandId, string IpAddress, string HostName);

public class NewPlayerInfo
{
}