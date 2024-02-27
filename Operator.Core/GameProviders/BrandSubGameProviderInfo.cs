using System;
using TGS.Core.Models.Enums;

namespace TGS.Services.Operator;

public record BrandSubGameProviderInfo(
	string Code,
	string Name,
	string Description,
	Guid GameProviderId,
	string GpCode,
	bool IsTransferWallet,
	Status Status,
	string Localization);