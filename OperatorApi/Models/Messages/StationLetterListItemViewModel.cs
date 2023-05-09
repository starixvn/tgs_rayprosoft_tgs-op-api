#nullable enable

namespace TGS.Apis.OperatorApi.Models.Messages;

public class StationLetterListItemViewModel
{
	public Guid Id { get; set; }

	public required string Sender { get; set; }

	public DateTime SentOn { get; set; }

	public required string Subject { get; set; }
}
