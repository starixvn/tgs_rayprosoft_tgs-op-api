using System;
using TGS.Core.Models.Enums;

namespace TGS.Services.Operator.ApplicationServices.Category
{
	public record GetRecommendedGamesByTypesQuery(
		Guid BrandId,
		int Limit,
		string CultureCode,
		PlatformType PlatformType,
		GameType[] Types);

}
