using System;

namespace TGS.Services.Operations.Models.Services;

public record ChangeAgentPasswordData
{
    public Guid Id { get; set; }
    public string Username { get; init; }
    public byte[] Salt { get; init; }
    public byte[] Password { get; init; }
    public DateTimeOffset UpdatedOn { get; init; }
}