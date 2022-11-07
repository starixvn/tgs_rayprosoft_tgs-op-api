using System.Globalization;
using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Announcement;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api")]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
public class AnnouncementController : ControllerBase
{
	[HttpGet("announcement/list")]
	[HttpGet("announcement/list/{language}")]
	public async ValueTask<AnnouncementResponse> GetAnnouncementsAsync(
		[FromServices] IQueryService<GetAnnouncementQuery, AnnouncementInfo> getAnnouncementQueryService)
	{
		var currentCultureName = CultureInfo.CurrentUICulture.Name;
		var brandId = HttpContext.Items.GetBrandId();

		var query = new GetAnnouncementQuery(brandId);

		var result = await getAnnouncementQueryService.QueryAsync(query).ToArrayAsync();

		var marquees = result
			.Where(a => a.Type == "Marquee")
			.OrderBy(a => a.DisplayOrder)
			.Select(item => new Announcement
			{
				Content = item.Content,
				From = item.From.ToUnixTimeMilliseconds(),
				To = item.To.ToUnixTimeMilliseconds(),
				Subject = item.Subject,
				Localization = item.Localization.ContainsKey(currentCultureName) == true
					? item.Localization[currentCultureName]
					: JsonDocument.Parse("{}"),
				Type = item.Type
			})
			.ToArray();

		var main = result
			.Where(a => a.Type == "Main")
			.OrderBy(a => a.CreatedOn)
			.Take(1)
			.Select(item => new Announcement
			{
				Content = item.Content,
				From = item.From.ToUnixTimeMilliseconds(),
				To = item.To.ToUnixTimeMilliseconds(),
				Subject = item.Subject,
				Localization = item.Localization.ContainsKey(currentCultureName) == true
					? item.Localization[currentCultureName]
					: JsonDocument.Parse("{}"),
				Type = item.Type
			})
			.ToArray();

		var deposit = result
			.Where(a => a.Type == "Deposit")
			.OrderBy(a => a.CreatedOn)
			.Take(1)
			.Select(item => new Announcement
			{
				Content = item.Content,
				From = item.From.ToUnixTimeMilliseconds(),
				To = item.To.ToUnixTimeMilliseconds(),
				Subject = item.Subject,
				Localization = item.Localization.ContainsKey(currentCultureName) == true
					? item.Localization[currentCultureName]
					: JsonDocument.Parse("{}"),
				Type = item.Type
			})
			.ToArray();

		return new AnnouncementResponse
		{
			Marquees = marquees,
			Mains = main,
			Deposits = deposit
		};
	}
}
