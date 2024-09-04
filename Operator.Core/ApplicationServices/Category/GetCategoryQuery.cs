using System;
using TGS.Core.Models.Enums;

namespace TGS.Services.Operator.ApplicationServices.Category;
public record GetCategoriesQuery(
	Guid BrandId,
	PlayerType PlayerType,
	PlatformType PlatformType,
	GameType Type,
	int PageSize,
	int PageIndex);
