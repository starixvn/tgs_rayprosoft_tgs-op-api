using System;
using TGS.Core.Models.Enums;

namespace TGS.Services.Operations.Models.Services;

public record PlayerLoginData
{
	public Guid BrandId { get; init; }
	public string Username { get; init; }
	public string PlayerCode { get; init; }
	public PlatformType PlatformType { get; init; }
	public string IpAddress { get; init; }
	public DateTimeOffset CreatedOn { get; init; }
	public DateTimeOffset ExpiredOn { get; init; }
}
