using System;

namespace TGS.Services.Operator;

public record SubGameProviderWithLocationDataInfo(
    Guid GameProviderId,
    string GpCode,
    string Code,
    string Name,
    string Description,
    string Localization);
