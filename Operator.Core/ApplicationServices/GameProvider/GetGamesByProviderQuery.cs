using System;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Tokens;

namespace TGS.Services.Operator.ApplicationServices.GameProvider
{
	public record GetGamesByProviderQuery(
		Guid BrandId,
		string CurrencyCode,
		string cultureCode,
		PlayerTokenData? PlayerTokenData,
		PlatformType PlatformType,
		int PageSize,
		int PageIndex,
		string[] ProviderCodes,
		byte OrderBy);
}
