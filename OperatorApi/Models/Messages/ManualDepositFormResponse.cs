using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record ManualDepositFormResponse
{
	[JsonPropertyName("bankaccount")]
	public string BankAccount { get; init; }

	[JsonPropertyName("bankaccountname")]
	public string BankAccountName { get; init; }

	[JsonPropertyName("amount")]
	public decimal Amount { get; init; }

	[JsonPropertyName("channelname")]
	public string ChannelName { get; init; }

	[JsonPropertyName("methodname")]
	public string MethodName { get; init; }

	[JsonPropertyName("bankname")]
	public string BankName { get; init; }
}
