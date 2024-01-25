using System;
using System.Collections.Generic;
using System.Threading;
using Tgs.ArticleManagement.Clients;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Articles;

internal class ListArticlesQueryService(IArticleManagementClient articleManagementClient) : IQueryService<ListArticlesQuery, ArticleListItem>
{
	private readonly IArticleManagementClient _articleManagementClient = articleManagementClient ?? throw new ArgumentNullException(nameof(articleManagementClient));

	public IAsyncEnumerable<ArticleListItem> QueryAsync(ListArticlesQuery query, CancellationToken cancellationToken = default)
		=> _articleManagementClient.ListArticleFormsAsync(query.BrandId, cancellationToken);
}
