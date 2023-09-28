#nullable enable

using Microsoft.AspNetCore.Mvc;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;
using Tgs.StationLetterService.Client;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Services.Operator.ApplicationServices.Brand;
using TGS.Services.Operator.ApplicationServices.Player;
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
		[FromQuery(Name = "unread")] bool onlyUnread = false,
		[FromQuery(Name = "page")] int page = 1,
		[FromQuery(Name = "rows")] int rows = 10)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData()!;

		var query = new GetStationLettersV2Query(playerTokenData.PlayerCode);

		var letters = queryService.QueryAsync(query, HttpContext.RequestAborted);

		if (onlyUnread)
			letters = letters.Where(l => l.HaveRead == false);

		var result = await letters
			.Select(letter => new StationLetterListItemViewModel
			{
				Id = letter.Id,
				Sender = letter.Sender,
				Subject = letter.Subject,
				SentOn = letter.SentOn,
				HaveRead = letter.HaveRead,
				Replied = letter.HaveRead
			})
			.ToArrayAsync(HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return new PagingResponse<StationLetterListItemViewModel>
		{
			TotalCount = result.Length,
			Items = result.Chunk(rows).Skip(page - 1).FirstOrDefault(),
		};
	}

	[HttpPut("{letterId}:reply", Name = "ReplyStationLetter")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async ValueTask ReplyLetterAsync(
		[FromServices] ICommandService<ReplyLetterCommand> replyLetterService,
		[FromServices] IGetService<GetPlayerMainDataQuery, PlayerInfo?> getPlayerInfoService,
		Guid letterId,
		[FromBody] ReplyStationLetterViewModel viewModel)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData()!;

		var playerInfo = await getPlayerInfoService.GetAsync(
			  new(playerTokenData.PlayerCode),
			  HttpContext.RequestAborted).ConfigureAwait(false);

		if (playerInfo is null)
			return;

		var command = new ReplyLetterCommand(
			playerTokenData.PlayerCode,
			letterId,
			viewModel.Content,
			playerInfo.UserName);

		await replyLetterService.ExecuteAsync(
			command,
			HttpContext.RequestAborted).ConfigureAwait(false);
	}

	[HttpPut(Name = "SendStationLetterToCustomerService")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task SendLetterAsync(
		[FromServices] IGetService<BrandQuery, Services.Operator.BrandInfo> getBrandService,
		[FromServices] IGetService<GetPlayerMainDataQuery, PlayerInfo?> getPlayerInfoService,
		[FromServices] ICommandService<SendStationLetterCommand> sendLetterService,
		CreateStationLetterViewModel viewModel)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData()!;
		var brandId = HttpContext.GetBrandId()!.Value;

		var brandInfo = await getBrandService.GetAsync(
			new(brandId),
			HttpContext.RequestAborted).ConfigureAwait(false);

		var playerInfo = await getPlayerInfoService.GetAsync(
			new(playerTokenData.PlayerCode),
			HttpContext.RequestAborted).ConfigureAwait(false);

		if (playerInfo is null)
			return;

		var command = new SendStationLetterCommand(
			playerTokenData.PlayerCode,
			new[] { brandInfo.Code },
			viewModel.Subject,
			viewModel.Content,
			playerInfo.UserName);

		await sendLetterService.ExecuteAsync(
			command,
			HttpContext.RequestAborted).ConfigureAwait(false);
	}

	[HttpPatch("{letterId}:read", Name = "SetStationLetterHaveRead")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public ValueTask SetLetterHaveReadAsync(
		[FromServices] ICommandService<SetHaveReadV2Command> setReadService,
		Guid letterId)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData()!;

		var command = new SetHaveReadV2Command(playerTokenData.PlayerCode, letterId);

		return setReadService.ExecuteAsync(command, HttpContext.RequestAborted);
	}
}
