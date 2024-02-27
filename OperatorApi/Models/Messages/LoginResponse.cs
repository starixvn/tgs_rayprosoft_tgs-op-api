using System.Text.Json.Serialization;
using TGS.Core.Models.Messages;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record LoginResponse : TgsResponse
{
	[JsonPropertyName("token")]
	public string PlayerToken { get; init; }

	/// <summary>
	/// unix timestamp to milliseconds
	/// </summary>
	[JsonPropertyName("logintime")]
	public long LoginTime { get; init; }
}