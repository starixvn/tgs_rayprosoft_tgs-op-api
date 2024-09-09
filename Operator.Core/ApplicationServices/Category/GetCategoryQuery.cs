using System;
using TGS.Core.Models.Enums;

namespace TGS.Services.Operator.ApplicationServices.Category;
public record GetCategoriesQuery(
	Guid BrandId,
	PlayerType PlayerType,
	PlatformType PlatformType,
	string CultureCode,
	int PageSize,
	int PageIndex,
	GameType[] Types);
