
namespace TGS.Apis.OperatorApi.Models.Messages.ReportAgentContribution;

public record AgentContributionReportResponse
{
    public IEnumerable<AgentContributionReportModel> Items { get; init; }

    public AgentContributionTotalModel Total { get; init; }

    public int TotalCount { get; init; }
}