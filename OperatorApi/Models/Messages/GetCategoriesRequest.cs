using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TGS.Core.Models.Enums;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record GetGameTypesRequest
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

	public GameType[] GameTypes { get; set; }

	public GameTag Tag { get; init; } = GameTag.None;

	public byte OrderBy { get; init; }
}
