using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class SetWeChatIdRequest
{
	[JsonPropertyName("wechat")]
	public string WebChatId { get; init; }
}
