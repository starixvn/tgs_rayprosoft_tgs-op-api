using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record GetGameUrlRequest
{
    [Required]
    [JsonPropertyName("gpcode")]
    [BindProperty(Name = "gpcode")]
    public string SubGameProviderCode { get; init; }

    [Required]
    [JsonPropertyName("gid")]
    [BindProperty(Name = "gid")]
    public int GameId { get; init; }

    [Required]
    [JsonPropertyName("language")]
    [BindProperty(Name = "language")]
    public string LanguageCode { get; init; }

    [JsonPropertyName("extends")]
    [BindProperty(SupportsGet = false)]
    public JsonDocument ExtendedData { get; init; }
}