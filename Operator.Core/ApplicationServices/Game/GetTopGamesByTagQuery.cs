using System;
using TGS.Core.Models.Enums;

namespace TGS.Services.Operator.ApplicationServices.Game
{
	public record GetTopGamesByTagQuery(
		Guid BrandId,
		PlatformType PlatformType,
		string CultureCode,
		int Limit,
		GameTag[] Tags);
}
