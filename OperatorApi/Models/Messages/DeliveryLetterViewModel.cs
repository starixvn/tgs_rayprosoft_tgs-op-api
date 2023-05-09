#nullable enable

using System.Text.Json;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class DeliveryLetterViewModel
{
	public Guid? ComeFrom { get; set; }

	public required string Content { get; set; }

	public required Guid Id { get; set; }

	public JsonDocument? Receiver { get; set; }

	public required string ReceiverType { get; set; }

	public required string Sender { get; set; }

	public DateTime SentOn { get; set; }

	public required string Subject { get; set; }
}
