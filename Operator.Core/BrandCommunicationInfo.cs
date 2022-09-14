namespace TGS.Services.Operator;

public record BrandCommunicationInfo
{
    public string CommunicationType { get; init; } = default!;
    public string Phone { get; init; } = default!;
    public string AppId { get; init; } = default!;
    public string QRCodeUrl { get; init; } = default!;
}
