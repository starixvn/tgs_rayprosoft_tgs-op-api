using System;

namespace TGS.Services.Operations.Models.Services;

public record ChangePlayerPasswordData
{
    public string Username { get; init; }
    public string PlayerCode { get; init; }
    public byte[] Salt { get; init; }
    public byte[] Password { get; init; }
    public DateTimeOffset UpdatedOn { get; init; }
}