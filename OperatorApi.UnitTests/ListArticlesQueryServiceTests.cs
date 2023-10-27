using NSubstitute;
using Tgs.ArticleManagement.Clients;
using TGS.Services.Operator.ApplicationServices.Articles;

namespace TGS.Tests.OperatorApi.UnitTests;

public class ListArticlesQueryServiceTests
{
	[Test]
	public async Task 取得上架文章列表()
	{
		var fakeArticleManagementClient = Substitute.For<IArticleManagementClient>();

		var sut = new ListArticlesQueryService(fakeArticleManagementClient);

		var query = new ListArticlesQuery(Guid.NewGuid());

		_ = await sut.QueryAsync(query).ToArrayAsync();

		_ = fakeArticleManagementClient.Received(1)
			.ListArticleFormsAsync(Arg.Is(query.BrandId));
	}
}
