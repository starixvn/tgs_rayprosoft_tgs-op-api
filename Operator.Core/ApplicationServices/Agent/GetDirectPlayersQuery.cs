using System;
using TGS.Core.Models.Enums;

namespace TGS.Services.Operator.ApplicationServices.Agent;

public record GetDirectPlayersQuery(Guid BrandId, Guid AgentId,string? PlayerUsername, PlayerStatus? PlayerStatus);
