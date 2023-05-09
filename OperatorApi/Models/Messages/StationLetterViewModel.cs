#nullable enable

namespace TGS.Apis.OperatorApi.Models.Messages;

public class StationLetterViewModel
{
	public required string Content { get; set; }

	public required bool HaveRead { get; set; }

	public required Guid Id { get; set; }

	public string? Quotation { get; set; }

	public required string Receiver { get; set; }

	public required bool Replied { get; set; }

	public required string Sender { get; set; }

	public required DateTime SentOn { get; set; }

	public required string Subject { get; set; }
}
