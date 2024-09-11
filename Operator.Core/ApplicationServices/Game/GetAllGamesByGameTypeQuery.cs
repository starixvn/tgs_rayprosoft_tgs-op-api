using System;
using TGS.Core.Models.Enums;

namespace TGS.Services.Operator.ApplicationServices.Game;

public record GetAllGamesByGameTypeQuery(
	Guid BrandId,
	PlayerType PlayerType,
	PlatformType PlatformType,
	Core.Models.Enums.GameType[] Types);