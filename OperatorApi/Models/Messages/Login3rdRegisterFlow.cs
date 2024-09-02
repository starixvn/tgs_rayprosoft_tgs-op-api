using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record Login3rdRegisterFlow
{
	[Required]
	[JsonPropertyName("username")]
	public string username { get; init; }

	[Required]
	[JsonPropertyName("testplayer")]
	public bool testplayer { get; init; }

	[Required]
	[JsonPropertyName("pwd")]
	public string pwd { get; init; }

	[Required]
	[JsonPropertyName("invitationcode")]
	public string invitationcode { get; init; }
}