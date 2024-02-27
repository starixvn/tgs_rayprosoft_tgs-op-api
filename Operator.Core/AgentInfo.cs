using System;
using TGS.Core.Models.Enums;

namespace TGS.Services.Operator;

public record AgentInfo(
	Guid Id,
	string UserName,
	string InvitationCode,
	AgentStatus Status);