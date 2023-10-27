#nullable enable

using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Tgs.ArticleManagement.Clients;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Services.Operator.ApplicationServices.Articles;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ArticleController : ControllerBase
{
	[HttpGet(Name = "ListArticles")]
	public IAsyncEnumerable<ArticleListItemViewModel> ListArticlesAsync(
		[FromServices] IQueryService<ListArticlesQuery, ArticleListItem> queryService,
		[FromQuery(Name = "take")] int? take = null)
	{
		var brandId = HttpContext.GetBrandId();

		if (brandId is null)
			return AsyncEnumerable.Empty<ArticleListItemViewModel>();

		var query = new ListArticlesQuery(brandId.Value);
		var currentCultureName = CultureInfo.CurrentUICulture.Name;

		var result = queryService.QueryAsync(query, HttpContext.RequestAborted);
		if (take is not null and > 0)
			result = result.Take(take.Value);

		return result
			.Where(item => item.LocalizedArticles.Count > 0)
			.Select(item => (
				Item: item,
				Article: item.LocalizedArticles.TryGetValue(currentCultureName, out var article)
					? article
					: item.LocalizedArticles["en-US"]))
			.Select(tup => new ArticleListItemViewModel
			{
				Id = tup.Item.FormId,
				Title = tup.Article.Title,
				Brief = tup.Article.Brief,
				UpdatedOn = tup.Item.UpdatedOn
			});
	}

	[HttpGet("{id:Guid}", Name = "GetArticle")]
	public async ValueTask<ArticleViewModel?> GetArticleAsync(
		[FromServices] IQueryService<ListArticlesQuery, ArticleListItem> queryService,
		Guid id)
	{
		var brandId = HttpContext.GetBrandId();

		if (brandId is null)
			return null;

		var query = new ListArticlesQuery(brandId.Value);
		var currentCultureName = CultureInfo.CurrentUICulture.Name;

		return await queryService.QueryAsync(query, HttpContext.RequestAborted)
			.Where(item => item.FormId == id)
			.Take(1)
			.Select(item => (
				Item: item,
				Article: item.LocalizedArticles.TryGetValue(currentCultureName, out var article)
					? article
					: item.LocalizedArticles["en-US"]))
			.Select(tup => new ArticleViewModel
			{
				Id = tup.Item.FormId,
				Title = tup.Article.Title,
				Brief = tup.Article.Brief,
				Content = tup.Article.Content,
				Writer = tup.Article.Writer,
				UpdatedOn = tup.Item.UpdatedOn
			})
			.FirstOrDefaultAsync(HttpContext.RequestAborted)
			.ConfigureAwait(false);
	}
}
