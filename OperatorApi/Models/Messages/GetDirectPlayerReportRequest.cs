#nullable enable

using TGS.Core.Models.Enums;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record GetDirectPlayerReportRequest : PagingRequest
{
	public string? PlayerUsername { get; set; }
	public PlayerStatus? Status { get; set; }
}