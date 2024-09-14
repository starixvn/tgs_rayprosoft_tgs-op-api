using System;
using TGS.Core.Models.Enums;

namespace TGS.Services.Operator.ApplicationServices.GameTypes;

public record GetGameTypesQuery(
	Guid BrandId,
	PlayerType PlayerType,
	PlatformType PlatformType,
	string CultureCode,
	int PageSize,
	int PageIndex,
	string[] ProviderCode,
	GameType[] Types,
	GameTag Tag,
	OrderType? OrderType);
