using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

#nullable enable

namespace TGS.Apis.OperatorApi.Models.Messages;

public record CreatePlayerRequest
{
	[JsonPropertyName("invitationcode")]
	public string? AgentInvitationCode { get; init; }

	[JsonPropertyName("confirmpwd")]
	public string? ConfirmPassword { get; init; }

	[JsonPropertyName("testplayer")]
	public bool IsTestPlayer { get; init; } = false;

	[Required]
	[JsonPropertyName("pwd")]
	public string Password { get; init; } = default!;

	[JsonPropertyName("upper")]
	public string? UpperPlayerCode { get; init; }

	[Required]
	[JsonPropertyName("username")]
	public string UserName { get; init; } = default!;
}
