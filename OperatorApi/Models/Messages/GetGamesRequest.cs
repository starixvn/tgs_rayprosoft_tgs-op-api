using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TGS.Core.Models.Enums;

namespace TGS.Apis.OperatorApi.Models.Messages;

[BindProperties(SupportsGet = true)]
public record GetGamesRequest
{
    [BindProperty(Name = "gpcode")]
    public string SubGameProviderCode { get; init; }

    [Required]
    [BindProperty(Name = "platformtype")]
    public PlatformType PlatformType { get; init; }

    [Required]
    [BindProperty(Name = "language")]
    public string LanguageCode { get; init; }

    [Required]
    [BindProperty(Name = "currency")]
    public string CurrencyCode { get; init; }

    [BindProperty(Name = "gametypes")]
    public GameType[] Types { get; init; }
}