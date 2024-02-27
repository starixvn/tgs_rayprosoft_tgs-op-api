#nullable enable

namespace TGS.Apis.OperatorApi.Models.Messages;

public class RouletteItemViewModel
{
	public decimal Bonus { get; set; }

	public Guid Id { get; set; }

	public string Title { get; set; } = default!;
}