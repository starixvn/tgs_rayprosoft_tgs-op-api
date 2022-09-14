using System;

namespace TGS.Services.Operator;

public record BannerInfo(
    Guid Id,
    string Title,
    string? Localization);
