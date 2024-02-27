using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using TGS.Core.Models.Enums;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record GetLobbyGameUrlRequest
{
	[Required]
	[JsonPropertyName("gpcode")]
	[BindProperty(Name = "gpcode")]
	public string SubGameProviderCode { get; init; }

	[Required]
	[JsonPropertyName("gtype")]
	[BindProperty(Name = "gtype")]
	public GameType GameType { get; init; }

	[Required]
	[JsonPropertyName("language")]
	[BindProperty(Name = "language")]
	public string LanguageCode { get; init; }

	[JsonPropertyName("extends")]
	[BindProperty(SupportsGet = false)]
	public JsonDocument Extends { get; set; }
}