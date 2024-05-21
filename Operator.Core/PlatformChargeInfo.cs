using System;

#nullable enable

namespace TGS.Services.Operator;

public record PlatformChargeInfo(
	Guid BrandId,
	decimal PlatformChargePercentage);