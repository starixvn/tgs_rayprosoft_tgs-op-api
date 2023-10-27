namespace TGS.Apis.OperatorApi.Models.Messages;

public class ArticleListItemViewModel
{
	public Guid Id { get; set; }

	public string Title { get; set; }

	public string Brief { get; set; }

	public DateTime UpdatedOn { get; set; }
}
