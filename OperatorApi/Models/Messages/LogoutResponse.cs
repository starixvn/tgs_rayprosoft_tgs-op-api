using System.Text.Json.Serialization;
using TGS.Core.Models.Messages;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record LogoutResponse : TgsResponse
{
	/// <summary>
	/// unix timestamp to milliseconds
	/// </summary>
	[JsonPropertyName("updatetime")]
	public long UpdateTime { get; init; }
}
