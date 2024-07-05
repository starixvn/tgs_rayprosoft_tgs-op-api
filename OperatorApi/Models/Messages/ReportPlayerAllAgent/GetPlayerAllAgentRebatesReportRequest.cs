#nullable enable
namespace TGS.Apis.OperatorApi.Models.Messages.ReportPlayerAllAgent;

public record GetPlayerAllAgentRebatesReportRequest : PagingRequest
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
}
