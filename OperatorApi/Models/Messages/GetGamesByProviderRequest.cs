using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TGS.Core.Models.Enums;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class GetGamesByProviderRequest
{
	public int PageIndex { get; init; }

	public int PageSize { get; init; }

	[Required]
	public PlatformType PlatformType { get; init; }

	[Required]
	[BindProperty(Name = "language", SupportsGet = true)]
	public string LanguageCode { get; init; }

	[Required]
	public string CurrencyCode { get; init; }


	[Required]
	[BindProperty(Name = "gpcode", SupportsGet = true)]
	public string[] ProviderCodes { get; init; }


	public byte OrderBy { get; init; }
}
