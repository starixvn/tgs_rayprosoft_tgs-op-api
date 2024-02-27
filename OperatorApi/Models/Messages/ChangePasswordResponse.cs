using System.Text.Json.Serialization;
using TGS.Core.Models.Messages;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record ChangePasswordResponse : TgsResponse
{
	[JsonPropertyName("playercode")]
	public string PlayerCode { get; init; }

	[JsonPropertyName("updatetime")]
	public DateTimeOffset UpdateTime { get; init; }
}