#nullable enable

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
	[HttpGet("{letterId}", Name = "GetPlayerStationLetterContent")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<StationLetterViewModel?> GetLetterAsync(
		[FromServices] IGetService<GetStationLetterQuery, StationLetter?> getLetterService,
		Guid letterId)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData()!;

		var query = new GetStationLetterQuery(letterId);

		return await getLetterService.GetAsync(query, HttpContext.RequestAborted)
			.AsTask()
			.ToAsyncEnumerable()
			.Where(letter => letter is not null)
			.Where(letter => letter!.Receiver == playerTokenData.PlayerCode)
			.Select(letter => letter!)
			.Select(letter => new StationLetterViewModel
			{
				Id = letter.Id,
				Receiver = letter.Receiver,
				SentOn = letter.SentOn,
				Content = letter.Content,
				HaveRead = letter.HaveRead,
				Replied = letter.Replied,
				Sender = letter.Sender,
				Subject = letter.Subject,
				Quotation = letter.Quotation
			})
			.FirstOrDefaultAsync(HttpContext.RequestAborted)
			.ConfigureAwait(false);
	}

	[HttpGet(Name = "GetPlayerStationLetters")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<PagingResponse<StationLetterListItemViewModel>> GetLettersAsync(
		[FromServices] IQueryService<GetStationLettersV2Query, StationLetter> queryService,
		[FromQuery(Name = "page")] int page = 1,
		[FromQuery(Name = "rows")] int rows = 10)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData()!;

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
