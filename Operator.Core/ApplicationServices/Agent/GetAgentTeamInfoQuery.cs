using System;

namespace TGS.Services.Operator.ApplicationServices.Agent;

public record GetAgentTeamInfoQuery(Guid BrandId, Guid AgentId);
