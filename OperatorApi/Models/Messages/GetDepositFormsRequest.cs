using Microsoft.AspNetCore.Mvc;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record GetDepositFormsRequest
{
	[FromQuery(Name = "status")]
	public FlowState? Status { get; init; }

	[FromQuery(Name = "begincreate")]
	public DateTimeOffset? BeginCreateOn { get; init; }

	[FromQuery(Name = "endcreate")]
	public DateTimeOffset? EndCreateOn { get; init; }

	[FromQuery(Name = "beginupdate")]
	public DateTimeOffset? BeginUpdateOn { get; init; }

	[FromQuery(Name = "endupdate")]
	public DateTimeOffset? EndUpdateOn { get; init; }
}
