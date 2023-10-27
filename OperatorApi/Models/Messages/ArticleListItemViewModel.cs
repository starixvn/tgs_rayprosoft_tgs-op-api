#nullable enable

namespace TGS.Apis.OperatorApi.Models.Messages;

public class ArticleListItemViewModel
{
	public required Guid Id { get; set; }

	public required string Title { get; set; }

	public required string Brief { get; set; }

	public required DateTime UpdatedOn { get; set; }
}
