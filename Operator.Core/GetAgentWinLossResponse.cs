using TGS.Core.Models.Messages;

namespace TGS.Services.Operator;

public class GetAgentWinLossResponse : MessageResponse
{
    public AgentWinLossModel[]? Items { get; init; }

    public AgentWinLossTotalModel? Total { get; init; }

    public int TotalCount { get; init; }
}