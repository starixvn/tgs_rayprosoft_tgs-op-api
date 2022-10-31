using System;

namespace TGS.Services.Operations.Models.Services;

public record PlayerLogoutData
{
	public string Username { get; init; }
	public string PlayerCode { get; init; }
	public Guid LoginRecordId { get; init; }
	public DateTimeOffset UpdatedOn { get; init; }
}
