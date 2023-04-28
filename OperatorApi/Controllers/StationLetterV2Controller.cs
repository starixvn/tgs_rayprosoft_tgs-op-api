using Microsoft.AspNetCore.Mvc;
using Tgs.ServiceUtility;
using Tgs.StationLetterService.Client;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Services.Operator.ApplicationServices.StationLetters;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/station-letter")]
[ApiController]
public class StationLetterV2Controller : ControllerBase
{
	[HttpGet(Name = "GetPlayerStationLetters")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<PagingResponse<StationLetterListItemViewModel>> GetLettersAsync(
		[FromServices] IQueryService<GetStationLettersV2Query, StationLetter> queryService,
		[FromQuery(Name = "page")] int page = 1,
		[FromQuery(Name = "rows")] int rows = 10)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var query = new GetStationLettersV2Query(playerTokenData.PlayerCode);

		var result = await queryService.QueryAsync(query, HttpContext.RequestAborted)
			.Select(letter => new StationLetterListItemViewModel
			{
				Id = letter.Id,
				Sender = letter.Sender,
				Subject = letter.Subject,
				SentOn = letter.SentOn
			})
			.ToArrayAsync(HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return new PagingResponse<StationLetterListItemViewModel>
		{
			TotalCount = result.Length,
			Items = result.Chunk(rows).Skip(page - 1).FirstOrDefault(),
		};
	}
}
