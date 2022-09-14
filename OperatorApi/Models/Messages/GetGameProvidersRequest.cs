using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TGS.Core.Models.Enums;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record GetGameProvidersRequest
{
    [Required]
    [BindProperty(Name = "platformtype", SupportsGet = true)]
    public PlatformType PlatformType { get; init; }

    [Required]
    [BindProperty(Name = "language", SupportsGet = true)]
    public string LanguageCode { get; init; }

    [Required]
    [BindProperty(Name = "currency", SupportsGet = true)]
    public string CurrencyCode { get; init; }
}