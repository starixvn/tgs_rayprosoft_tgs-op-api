using TGS.Core.Models.Enums;

namespace TGS.Services.Operator;

public record GameDataInfo(
	int Id,
	string Code,
	string Name,
	string Description,
	string SubGameProviderCode,
	GameStatus Status,
	Core.Models.Enums.GameType Type,
	GamePlatformType PlatformType,
	bool AvailableToBrand,
	string AvailableCurrencies,
	string Localization);