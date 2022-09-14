using System;
using TGS.Core.Models.Enums;

namespace TGS.Services.Operations.Models.Services;

public record AgentLoginData
{
    public Guid BrandId { get; init; }
    public string Username { get; init; }
    public Guid AgentId { get; init; }
    public string IpAddress { get; init; }
    public DateTimeOffset CreatedOn { get; init; }
    public DateTimeOffset ExpiredOn { get; init; }
}