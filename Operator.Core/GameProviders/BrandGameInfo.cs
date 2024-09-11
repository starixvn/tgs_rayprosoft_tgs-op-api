using System;
using TGS.Core.Models.Enums;

namespace TGS.Services.Operator;

public record BrandGameInfo(
	int Id,
	string Code,
	string Name,
	string Description,
	string SubGpCode,
	Guid GpId,
	string GpCode,
	GameStatus Status,
	Core.Models.Enums.GameType Type,
	GamePlatformType PlatformType,
	bool AvailableToBrand,
	string AvailableCurrencies,
	string Localization,
	GameTag Tag,
	int DisplayOrder);