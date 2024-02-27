using System;

#nullable enable

namespace TGS.Services.Operator.ApplicationServices.Player;

public record CreatePlayerWithAgentInvitationCodeCommand(
	Guid BrandId,
	string UserName,
	string Password,
	string CurrencyCode,
	string AgentInvitationCode,
	bool IsTestPlayer);